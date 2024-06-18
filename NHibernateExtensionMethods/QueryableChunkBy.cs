using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using NHibernate.Util;
using NHibernate;
using System.Reflection;

namespace NHibernateExtensionMethods
{
    /// <summary>
    /// Extensão de método para incluir na <see cref="IQueryable"/> clausula 'IN' agrupada por partes
    /// </summary>
    public static class QueryableChunkBy
    {
        private const int NUMBER_OF_LINES_ORACLE = 999;
        private const int NUMBER_OF_LINES = 1900;

        /// <summary>
        /// Retornar <see cref="IQueryable"/> com clausula 'IN' agrupada por partes conforme a quantidade informada.
        /// </summary>
        /// <param name="queryable"/>
        /// <param name="property"/>
        /// <param name="value"/>
        /// <remarks>
        /// Ex SQL: Select Id, Descricao From Tabela Where (Id In (1,2,3,4...999) or Id In (1000,1001,1002,...1500))
        /// </remarks>
        public static IQueryable<TSource> ChunkBy<TSource, TKey>(
            this IQueryable<TSource> queryable,
            Expression<Func<TSource, TKey>> property,
            IEnumerable<TKey> value)
        {
            var session = queryable.Provider
                .GetType()
                .GetProperty("Session", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(queryable.Provider, null) as ISession;

            var dialect = (session.SessionFactory as NHibernate.Impl.SessionFactoryImpl).Dialect;

            var itens = dialect.ToString().ToUpper().Contains("ORACLE")
                ? NUMBER_OF_LINES_ORACLE
                : NUMBER_OF_LINES;

            if (!value?.Any() ?? false)
                return queryable;

            if (!(property?.Body is MemberExpression))
                throw new ArgumentException("Parametro 'expression' invalida.");

            var partes = value
                .Distinct()
                .Select((s, index) => new
                {
                    Valor = s,
                    Parte = index / itens
                })
                .GroupBy(g => g.Parte);

            var expressoes = new List<Expression<Func<TSource, bool>>>();

            foreach (var item in partes)
                expressoes.Add(Expression.Lambda<Func<TSource, bool>>(
                    Expression.Call(
                        typeof(Enumerable),
                        nameof(string.Contains),
                        new[] { typeof(TKey) },
                        Expression.Constant(item
                            .Select(s => s.Valor)
                            .ToArray()),
                        property.Body),
                        property.Parameters));

            Expression<Func<TSource, bool>> expression = null;


            if (expressoes.Skip(1) is var expressionRight && expressionRight.Any())
            {
                var expressionsLeft = expressoes
                    .FirstOrDefault();

                foreach (var item in expressionRight)
                {
                    expression = Expression.Lambda<Func<TSource, bool>>(
                        Expression.Or(
                            expressionsLeft.Body,
                            item.Body),
                        expressionsLeft.Parameters);

                    expressionsLeft = expression;
                }
            }

            var retorno = queryable
                .Where(expression ?? expressoes.FirstOrDefault());

            return retorno;
        }
    }
}

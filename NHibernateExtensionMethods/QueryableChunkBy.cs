using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;

namespace NHibernateExtensionMethods
{
    /// <summary>
    /// Extensão de método para incluir na <see cref="IQueryable"/> clausula 'IN' agrupada por partes
    /// </summary>
    public static class QueryableChunkBy
    {
        /// <summary>
        /// Retornar <see cref="IQueryable"/> com clausula 'IN' agrupada por partes conforme a quantidade informada.
        /// </summary>
        /// <param name="queryable"/>
        /// <param name="propriedade">Propriedade</param>
        /// <param name="valores">Valores</param>
        /// <param name="Itens">Quantidade</param>
        /// <remarks>
        /// Ex SQL: Select Id, Descricao From Tabela Where (Id In (1,2,3,4...999) or Id In (1000,1001,1002,...1500))
        /// </remarks>
        public static IQueryable<TSource> ChunkBy<TSource, TKey>(
            this IQueryable<TSource> queryable,
            Expression<Func<TSource, TKey>> propriedade,
            IEnumerable<TKey> valores,
            int Itens = 999)
        {
            if (!valores?.Any() ?? false)
                return queryable;

            if (!(propriedade?.Body is MemberExpression))
                throw new ArgumentException("Parametro 'expression' invalida.");

            var partes = valores
                .Distinct()
                .Select((s, index) => new
                {
                    Valor = s,
                    Parte = index / Itens
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
                        propriedade.Body),
                        propriedade.Parameters));

            Expression<Func<TSource, bool>> expressao = null;
            if (expressoes.Skip(1) is var expressoesDireita && expressoesDireita.Any())
            {
                var expressaoEsquerda = expressoes
                    .FirstOrDefault();

                foreach (var item in expressoesDireita)
                {
                    expressao = Expression.Lambda<Func<TSource, bool>>(
                        Expression.Or(
                            expressaoEsquerda.Body,
                            item.Body),
                        expressaoEsquerda.Parameters);

                    expressaoEsquerda = expressao;
                }
            }

            var retorno = queryable
                .Where(expressao ?? expressoes.FirstOrDefault());

            return retorno;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Models
{
    public class PersistedEntity<TEntity, TKey>
    {
        public TKey Id { get; set; }
        public TEntity Entity { get; set; }

        public PersistedEntity()
        {
        }

        public PersistedEntity(TEntity entity)
        {
            Entity = entity;
        }
    }
}

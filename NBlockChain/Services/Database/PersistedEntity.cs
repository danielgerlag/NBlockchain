using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Services.Database
{
    public class PersistedEntity<TEntity, TKey, TStats>
        where TStats : new()
    {
        public TKey Id { get; set; }
        public TEntity Entity { get; set; }
        public TStats Statistics { get; set; }

        public PersistedEntity()
        {
            Statistics = new TStats();
        }

        public PersistedEntity(TEntity entity)
        {
            Entity = entity;
            Statistics = new TStats();
        }
    }

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

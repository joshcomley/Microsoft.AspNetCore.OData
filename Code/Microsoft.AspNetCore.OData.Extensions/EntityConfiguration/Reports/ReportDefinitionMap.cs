using System;
using System.Collections.Generic;
using System.Linq;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports
{
    public class ReportDefinitionMap<TEntity> : IReportDefintionMap
    {
        private const string DefaultGuid = "{E0B2578C-F44D-43B2-BBDE-820F3FD5BD8E}";
        private readonly Dictionary<string, ReportDefinition<TEntity>> _map = new Dictionary<string, ReportDefinition<TEntity>>();
        public IReadOnlyCollection<ReportDefinition<TEntity>>  Reports => _map.Values.ToList();
        IReadOnlyCollection<IReportDefinition> IReportDefintionMap.Reports => Reports;

        public ReportDefinition<TEntity> GetDefault()
        {
            return Get(DefaultGuid);
        }

        public ReportDefinition<TEntity> Get(string name)
        {
            if (_map.ContainsKey(name))
            {
                return _map[name];
            }

            return null;
        }

        public ReportDefinitionMap<TEntity> DefineDefault(Action<ReportDefinition<TEntity>> fields)
        {
            return Define(DefaultGuid, fields);
        }

        public ReportDefinitionMap<TEntity> Define(string name, Action<ReportDefinition<TEntity>> fields)
        {
            if (!_map.ContainsKey(name))
            {
                _map.Add(name, new ReportDefinition<TEntity>());
            }

            fields(_map[name]);
            return this;
        }

        public ReportDefinitionMap<TEntity> Remove(string name)
        {
            if (_map.ContainsKey(name))
            {
                _map.Remove(name);
            }
            return this;
        }

        public ReportDefinitionMap<TEntity> RemoveDefault()
        {
            return Remove(DefaultGuid);
        }
    }
}
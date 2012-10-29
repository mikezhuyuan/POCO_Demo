using System;
using System.Collections.Generic;
using System.Data;
using EF.Frameworks.Common.ConfigurationEF;
using EF.Frameworks.Common.DataEF.SqlClientEF;

namespace POCO_Demo
{
    public interface IObjectAssemblerManager
    {
        IObjectAssembler<T> GetAssembler<T>();

        /// <summary>
        /// facade methods
        /// </summary>
        T Create<T>(IDataReader dr);

        T Create<T>(IConfigurationContext config, IEFSqlCommand cmd);
        IEnumerable<T> CreateList<T>(IDataReader dr);
        IEnumerable<T> CreateList<T>(IConfigurationContext config, IEFSqlCommand cmd);

        /// <summary>
        /// Multi-DataResutls
        /// </summary>
        void CreateMany(IDataReader dr, params Action<ObjectAssemblerManager.MultipleResultAssembler>[] assemblers);

        void CreateMany(IConfigurationContext config, IEFSqlCommand cmd,
                        params Action<ObjectAssemblerManager.MultipleResultAssembler>[] assemblers);
    }
}
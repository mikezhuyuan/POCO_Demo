using System.Collections.Generic;
using System.Data;

namespace POCO_Demo
{
    public interface IObjectAssembler<T>
    {
        T Create(IDataReader dr);
        IEnumerable<T> CreateList(IDataReader dr);
    }    
}

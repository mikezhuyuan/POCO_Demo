using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace POCO_Demo
{
    public class Assignment
    {
        public int Assignment_id { get; internal set; }
        public string Title { get; internal set; }
        public int Parent_id { get; internal set; }
        public AssignmentType AssignmentType { get; internal set; }

        public override string ToString()
        {
            return string.Format("#{0}.{1}: {2} -> {3}", Assignment_id, AssignmentType, Title, Parent_id);
        }
    }

    public enum AssignmentType
    {
        CC,
        PL,
        GU,
        SU
    }
}

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
        public AssignmentType AssignmentType { get; private set; }

        public override string ToString()
        {
            return string.Format("#{0}.{1}: {2}", Assignment_id, AssignmentType, Title);
        }

        #region Non-Public
        internal string AssignmentTypeCode
        {
            set
            {
                AssignmentType = (AssignmentType)Enum.Parse(typeof(AssignmentType), value);
            }
        }
        #endregion
    }

    public enum AssignmentType
    {
        CC,
        PL,
        GU,
        SU
    }
}

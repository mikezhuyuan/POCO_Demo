using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EF.Frameworks.Common.ConfigurationEF;
using EF.Frameworks.Common.DataEF.SqlClientEF;

namespace POCO_Demo
{
    class MyFactory : ObjectAssemblerFactory
    {
        protected override void OnMapping(MappingBuilder builder)
        {
            builder.Entity<Assignment>()
                   .PropertyName(_ => _.Parent_id, "ParentAssignment_id")
                   .PropertyValue(_ => _.AssignmentType, "AssignmentTypeCode", (string code) => (AssignmentType)Enum.Parse(typeof(AssignmentType), code));
        }
    }

    class Program
    {
        static IConfigurationContext Config = new ConfigurationContext("EFSchools.Englishtown.ELab.Admin.Services.*");
        static ObjectAssemblerFactory factory = new MyFactory();
        static void Main(string[] args)
        {
            var assignment = GetPOCO2(); //GetFromSO, GetPOCO, GetScalar, GetTuple, GetPOCO2
            
            Console.WriteLine(assignment);            
            Console.ReadLine();
        }

        static int GetScalar()
        {            
            using (var cmd = new Assignment_Load_p())
            {
                cmd.Parameters.Assignment_id = 2;

                return factory.Get<int>().Create(Config, cmd);
            }
        }

        static Tuple<int, string> GetTuple()
        {
            using (var cmd = new Assignment_Load_p())
            {
                cmd.Parameters.Assignment_id = 2;

                return factory.Get<Tuple<int, string>>().Create(Config, cmd);
            }
        }

        static Assignment GetPOCO()
        {
            using (var cmd = new Assignment_Load_p())
            {
                cmd.Parameters.Assignment_id = 100;

                return factory.Get<Assignment>().Create(Config, cmd);
            }
        }

        static Assignment2 GetPOCO2()
        {
            using (var cmd = new Assignment_Load_p())
            {
                cmd.Parameters.Assignment_id = 100;

                return factory.Get<Assignment2>().Create(Config, cmd);
            }
        }

        static Assignment GetFromSO()
        {
            Assignment assignment = null;
            using (var cmd = new Assignment_Load_p())
            {
                cmd.Parameters.Assignment_id = 1;

                using (var sm = new SqlConnectionManager())
                using (var dr = sm.ExecuteReader(cmd, Config))
                {
                    var lst = AssignmentInfoAssembler.CreateList(dr);
                    if (lst != null && lst.Count > 0)
                    {
                        assignment = ConvertFrom(lst[0]);                        
                    }
                }
            }

            return assignment;
        }

        static Assignment ConvertFrom(AssignmentInfo ainfo)
        {
            return new Assignment
            {
                Assignment_id = ainfo.Assignment_id,
                Title = ainfo.Title,                
            };
        }

        public class Assignment2
        {
            public int Assignment_id { get; set; }

            public override string ToString()
            {
                return Assignment_id.ToString();
            }
        }
    }
}

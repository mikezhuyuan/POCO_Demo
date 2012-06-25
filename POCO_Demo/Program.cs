using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EF.Frameworks.Common.ConfigurationEF;
using EF.Frameworks.Common.DataEF.SqlClientEF;

namespace POCO_Demo
{
    class Program
    {
        static IConfigurationContext Config = new ConfigurationContext("EFSchools.Englishtown.ELab.Admin.Services.*");

        static void Main(string[] args)
        {
            var assignment = GetPOCO(); //GetFromSO, GetPOCO

            Console.WriteLine(assignment);            
            Console.ReadLine();
        }

        static Assignment GetPOCO()
        {
            using (var cmd = new Assignment_Load_p())
            {
                cmd.Parameters.Assignment_id = 2;

                return ObjectAssembler<Assignment>.Create(Config, cmd);
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
                AssignmentTypeCode = ainfo.AssignmentTypeCode,
            };
        }
    }
}

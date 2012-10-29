using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using EF.Frameworks.Common.ConfigurationEF;
using EF.Frameworks.Common.DataEF.SqlClientEF;

namespace POCO_Demo
{
    //domain object
    public class Assignment
    {
        public int Assignment_id { get; internal set; }
        public string Title { get; internal set; }
        public int? Parent_id { get; internal set; }
        public AssignmentType? AssignmentType { get; internal set; }
        
        public override string ToString()
        {
            return string.Format("id: {0}, type: {1}, title: {2}, parent: {3}", Assignment_id, AssignmentType, Title, Parent_id);
        }
    }
    
    public enum AssignmentType
    {
        CC,
        PL,
        GU,
        SU
    }

    //mapping
    class MyObjectAssemblerManager : ObjectAssemblerManager
    {
        protected override void OnMapping(MappingBuilder builder)
        {
            builder.Entity<Assignment>()                   
                   .PropertyName(_ => _.Parent_id, "ParentAssignment_id")
                   .PropertyValue(_ => _.AssignmentType, "AssignmentTypeCode", 
                                 (string code) => (AssignmentType)Enum.Parse(typeof(AssignmentType), code))
                   ;

            //.Create(reader=>new Assignment(){Assignment_id = (int)reader["Assignment_id"]})
            //.IgnoreProperty(_ => _.Title)
            //.IgnoreProperty(_ => _.Parent_id)
            //.IgnoreProperty(_ => _.AssignmentType)
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationContext("EFSchools.Englishtown.ELab.Admin.Services.*");
            var manager = new MyObjectAssemblerManager();

            //1. Assemble object
            //2. Assemble scalar
            //3. Assemble tuple
            //4. Performance

            //var watch = Stopwatch.StartNew();
            
            //for (int i = 0; i < 1000; i++)
            //{
            //    //GetFromAssembler(manager, config);    
            //    //GetFromSO(config); // 2513
            //}

            //watch.Stop();
            //Console.WriteLine(watch.ElapsedMilliseconds);

            Console.ReadLine();
        }

        static Assignment GetFromAssembler(MyObjectAssemblerManager manager, ConfigurationContext config)
        {
            using (var cmd = new Assignment_Load_p())
            {
                cmd.Parameters.Assignment_id = 99;

                return manager.Create<Assignment>(config, cmd);
            }
        }

        static Assignment GetFromSO(ConfigurationContext config)
        {
            using (var cmd = new Assignment_Load_p())
            {
                cmd.Parameters.Assignment_id = 99;

                using (var sm = new SqlConnectionManager())
                using (var dr = sm.ExecuteReader(cmd, config))
                {
                    var data = AssignmentInfoAssembler.CreateList(dr)[0];

                    return new Assignment
                    {
                        Assignment_id = data.Assignment_id,
                        AssignmentType = (AssignmentType)Enum.Parse(typeof(AssignmentType), data.AssignmentTypeCode),
                        Parent_id = data.ParentAssignment_id,
                        Title = data.Title,
                    };
                }
            }
        }

        static void WriteLine(object obj)
        {
            System.Console.WriteLine(obj);
        }
    }
}

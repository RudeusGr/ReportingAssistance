using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportingAssistance.Model
{
    public class Employee
    {
        public Employee(int id, string name)
        {
            Id = id;
            Bulk = 0;
            Name = name;
            Assistance = 0;
            Delays = 0;
            DicRouteDate = new();
        }
        public void AssistancesIncremente()
        {
            this.Assistance++;
        }
        public void DelaysIncremente()
        {
            this.Delays++;
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public int Bulk { get; set; }
        public int Assistance { get; set; }
        public int Delays { get; set; }
        public Dictionary<string, int> DicRouteDate { get; set; }
    }

}

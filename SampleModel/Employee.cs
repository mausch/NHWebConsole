using System.Collections.Generic;

namespace SampleModel {
    public class Employee {
        public virtual int Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }

        public virtual string FullName {
            get { return FirstName + " " + LastName; }
        }

        public virtual IList<Order> Orders { get; set; }
    }
}
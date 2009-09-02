using Iesi.Collections.Generic;

namespace SampleModel {
    public class Territory {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual ISet<Employee> Employees { get; set; }
    }
}
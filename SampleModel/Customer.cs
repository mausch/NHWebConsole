using System.Collections.Generic;

namespace SampleModel {
    public class Customer {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string Title { get; set; }

        public virtual IList<Order> Orders { get; set; }
    }
}
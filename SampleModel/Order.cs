using System;

namespace SampleModel {
    public class Order {
        public virtual int Id { get; set; }
        public virtual DateTime OrderDate { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual Employee Employee { get; set; }
    }
}
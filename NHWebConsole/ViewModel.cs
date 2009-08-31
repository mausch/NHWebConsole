using System.Collections.Generic;

namespace NHWebConsole {
    public class ViewModel {
        public string Url { get; set; }
        public string Hql { get; set; }
        public int? MaxResults { get; set; }
        public int? FirstResult { get; set; }
        public ICollection<ICollection<KeyValuePair<string, string>>> Results { get; set; }
        public string Error { get; set; }
        public string NextPageUrl { get; set; }
        public string PrevPageUrl { get; set; }
    }
}
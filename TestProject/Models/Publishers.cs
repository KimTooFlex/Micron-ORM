using Micron;
using System;
using System.Collections.Generic;

namespace Data.Models
{
/***PUBLISHER MODEL***/
  [Table("publishers")]
 public partial class Publisher : IMicron
 {
        [Primary]
        public Int32 publisherID {get; set;}
        public String publisherName {get; set;}
        public String email {get; set;}
        public String website {get; set;}
 }
}

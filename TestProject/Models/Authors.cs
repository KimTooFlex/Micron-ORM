using Micron;
using System;
using System.Collections.Generic;

namespace Data.Models
{
/***AUTHOR MODEL***/
  [Table("authors")]
 public partial class Author : IMicron
 {
        [Primary]
        public Int32 authorID {get; set;}
        public String frstName {get; set;}
        public String lastName {get; set;}
 }
}

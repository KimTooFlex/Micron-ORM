using Micron;
using System;
using System.Collections.Generic;

namespace Data.Models
{
/***COAUTHOR MODEL***/
  [Table("coauthors")]
 public partial class Coauthor : IMicron
 {
        [Primary]
        [Foreign(typeof(Author))]
        public Int32 id {get; set;}
 }
}

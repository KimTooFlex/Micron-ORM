using Micron;
using System;
using System.Collections.Generic;

namespace Data.Models
{
/***BOOK MODEL***/
  [Table("books")]
 public partial class Book : IMicron
 {
        [Primary]
        public Int32 id {get; set;}
        [Foreign(typeof(Author))]
        public Int32 authorID {get; set;}
        public Int32 CoAuthor {get; set;}
        public String isbn {get; set;}
        public String bookTitle {get; set;}
        public Int32 editionNumber {get; set;}
        public String copyright {get; set;}
        [Foreign(typeof(Publisher))]
        public Int32 publisherID {get; set;}
        public String imageFIle {get; set;}
        public Double price {get; set;}
 }
}

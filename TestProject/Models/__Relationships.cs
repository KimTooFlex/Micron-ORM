using Micron;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Models
{

#region AUTHORS
 public partial class Author
 {
public MicronDbContext DefaultDBContext { get; set; }

   public bool HasBooks(string where="true") {return DefaultDBContext.GetRecords<Book>("authorID = "+this.authorID+" AND "+where+" LIMIT 1").Count()>0;}
   public IEnumerable<Book> GetBooks(string where="true") {return DefaultDBContext.GetRecords<Book>("authorID = "+this.authorID+" AND "+where);}
    public void AddBook(Book model) { model.SetAuthor(this); }
    public void AddBooks(IEnumerable<Book> models) {foreach(var model in models) model.SetAuthor(this); }
   public bool HasCoauthor(string where="true") {return DefaultDBContext.GetRecords<Coauthor>("id = "+this.authorID+" AND "+where+" LIMIT 1").Count()>0;}
   public  Coauthor  GetCoauthor(string where="true") {return DefaultDBContext.GetRecords<Coauthor>("id = "+this.authorID+" AND "+where).FirstOrDefault() ;}
    public void SetCoauthor(Coauthor model) { model.SetAuthor(this); }

 }
#endregion
#region BOOKS
 public partial class Book
 {
public MicronDbContext DefaultDBContext { get; set; }
  public  Author GetAuthor() { return DefaultDBContext.GetRecord<Author>(this.authorID); }
   public void SetAuthor(Author model)  {  DefaultDBContext.SetRelation(this, model);}
  public  Publisher GetPublisher() { return DefaultDBContext.GetRecord<Publisher>(this.publisherID); }
   public void SetPublisher(Publisher model)  {  DefaultDBContext.SetRelation(this, model);}


 }
#endregion
#region COAUTHORS
 public partial class Coauthor
 {
public MicronDbContext DefaultDBContext { get; set; }
  public  Author GetAuthor() { return DefaultDBContext.GetRecord<Author>(this.id); }
   public void SetAuthor(Author model)  {  DefaultDBContext.SetRelation(this, model);}


 }
#endregion
#region PUBLISHERS
 public partial class Publisher
 {
public MicronDbContext DefaultDBContext { get; set; }

   public bool HasBooks(string where="true") {return DefaultDBContext.GetRecords<Book>("publisherID = "+this.publisherID+" AND "+where+" LIMIT 1").Count()>0;}
   public IEnumerable<Book> GetBooks(string where="true") {return DefaultDBContext.GetRecords<Book>("publisherID = "+this.publisherID+" AND "+where);}
    public void AddBook(Book model) { model.SetPublisher(this); }
    public void AddBooks(IEnumerable<Book> models) {foreach(var model in models) model.SetPublisher(this); }

 }
#endregion

}

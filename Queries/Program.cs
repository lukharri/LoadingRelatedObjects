using System.Linq;
using System;
using System.Data.Entity;

namespace Queries
{
    class Program
    {
        static void Main(string[] args)
        {
           /****************************** LAZY LOADING ******************************************/
           /**************************************************************************************/
           /*
            * Good solution to optimize an applicatioin's responsiveness in certain situations
            * But it can lead to the 'N + 1' problem which decreases performance
            * EX - get a course and display its tags
            * Any singleton method in a query is executed immediately (Single, SingleOrDefault,
            *   First, FirstOrDefault, Count, Min, Max, Average, etc...
            * But the course object doesn't have it tags initialized. When course.Tags is reached
            *   in the foreach block EF executes the query. Related objects are not loaded immediately
            *   but on demand when they are accessed.
            * BEST PRACTICES
            *   1. Desktop Apps - when loading a graph object is costly - for ex on app start up you want to load a
            *      bunch of related objects which can cause delay in app start up - use lazy loading
            *      to load the objects on demand as user uses app - 
            *   2. Avoid in web applications - b/x the response to a request requires knowing ahead
            *      of time what data to return to the client. Brand new requests will be made so
            *      you're not going to have objects in memory whose properties can be accessed. Lazy
            *      loading can cause unecessary round trips to the database which will decrease performance.
            *      Use configuration in the context (see PlutoContext) to prevent lazy loading and don't use 
            *      virtual keyword as it doesn't provide any benefit.      
            */
            var context = new PlutoContext();

            var course = context.Courses.Single(c => c.Id == 2);

            foreach (var t in course.Tags)
                Console.WriteLine(t.Name);


            /****************************** N + 1 Problem *****************************************/
            /**************************************************************************************/
            /* 
             * PROBLEM: To get N entities and their related entities you could end up with N+1 queries
             * EX - get all courses in the database and display each course's name and author
             * 1 query gets all the courses
             * 1 query is run (for each course) to get the course author
             * With N different authors you will have N+1 queries
             * 
             */
            var courses = context.Courses.ToList();

            foreach (var c in courses)
            {
                Console.WriteLine("{0} by {1}", c.Name, c.Author.Name);
            }


            /****************************** EAGER LOADING *****************************************/
            /**************************************************************************************/
            /* 
             * Joins the related tables and queries all the data in one round-trip
             * Opposite of Lazy Loading - load entities immediately to prevent unecessary queries to 
             * the database. 
             * Use the Include() method to implement eager loading
             *  Has 2 OVERLOADS - lambda expression or string
             *  STRING overload
             *    The query EF generates will join the Courses and Authors tables to eager load all the
             *    courses and their authors. 
             *    Problem with using string overload is it is a 'magic' string that doesn't change if
             *    properties are renamed and the code will break. Avoid using the string overload
             *  LAMBDA expression overload
             *    If author is renamed you will get a compile time error but you can use rename 
             *    refactoring to change author property in the class and in the lambda expression
             *    which can't be done using 'magic' strings. 
             */

            // var courses2 = context.Courses.Include("Author").ToList();
            var courses2 = context.Courses.Include(c => c.Author).ToList();

            foreach (var c in courses2)
            {
                Console.WriteLine("{0} by {1}", c.Name, c.Author.Name);
            }


            /************************ EAGER LOADING w/MULTIPLE LEVELS *****************************/
            /**************************************************************************************/
            /* 
             * Good or bad thing depending on situation
             * Using to many Include() methods make the queries more and more complex and stores
             *   many objects in memory that are not needed immediately
             * 
             * SINGLE PROPERTIES
             * EX - load all courses with their authors
             *    - each author has an address and you want to load that too
             *    - since address is a single property you can add it to the lambda expression
             *    - context.Courses.Include(c => c.Author.Address);
             *    
             * COLLECTION PROPERTIES
             * EX - load all courses and their tags
             *    - but each tag also has a moderator
             *    - tricky part is Tags is a collection and doesn't have the Moderator property
             *    - use the Select method to access a Tag object and reference the Moderator property w/lambda expression
             *    - context.Courses.Include(a => a.Tags.Select(t => t.Moderator)); 
             */


            /********************************** EXPLICIT LOADING **********************************/
            /**************************************************************************************/
            /* 
             * Useful when you need to use eager loading but your queries are getting too complex/bulky
             *   b/c of the number of Include() methods. 
             * Similar to eager loading in that you tell EF what should be loaded ahead of time
             *   but uses multiple queries and multiple round-trips depending on the number of explicit loads
             * Can apply filters to the related objects
             * 
             * To convert from eager to explicit remove Include() method/s and explicitly load related objects. 
             * Results in more trips to the database but can be more efficient than a very complex/bulky  
             *   eager loading query.
             * 
             */
            
            // Eager loading ex
            var author = context.Authors.Include(a => a.Courses).Single(a => a.Id == 1);
            
            // Make explicit - remove Include() and explicitly load the courses for the author
            var author2 = context.Authors.Single(a => a.Id == 1);

            // 2 WAYS to load explicitly
            // MSDN way
            // CONS - you need to remember more of the API of dbContext (all the methods)
            //      - only works for single entries
            //      - if the original query returned a list of authors you cannot use this approach
            //      - applying a filter to get the author's 'free' courses requires to more method calls 
            //        Query() and Where()
            context.Entry(author).Collection(a => a.Courses).Query().Where(c => c.FullPrice == 0).Load();

            // Better way - less noisy 
            context.Courses.Where(c => c.AuthorId == author.Id && c.FullPrice == 0).Load();

            foreach (var c in author.Courses)
            {
                Console.WriteLine("{0}", course.Name);
            }
        }
    }
}

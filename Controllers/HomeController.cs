using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MyApp.Models;
using System;
using System.Collections;
using System.Diagnostics;
using System.Xml.Linq;

namespace MyApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMongoDatabase _db;
        private readonly IMongoCollection<Entity> _collection;
        private readonly GridFSBucket bucket;


        public HomeController()
        {
            var mongoClient = new MongoClient("mongodb://localhost:27017");
            _db = mongoClient.GetDatabase("Blogs");
            _collection = _db.GetCollection<Entity>("blog");
            bucket = new GridFSBucket(_db, new GridFSBucketOptions
            {
                BucketName = "blogs",
                ChunkSizeBytes = 1048576,
                WriteConcern = WriteConcern.WMajority,
                ReadPreference = ReadPreference.Secondary
            });
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult List()
        {
            IEnumerable<Entity> EntityList = _collection.Find(a => true).ToList();
            List<Pass> PassList = new List<Pass>();
            foreach (var item in EntityList)
            {
                var title = item.Title;
                var description = item.Description;
                byte[] photo =  bucket.DownloadAsBytesByName(item.Id);
                Pass blog = new Pass();
                blog.Title = title;
                blog.Description = description;
                blog.Id= item.Id;
                blog.Photo = Convert.ToBase64String(photo, 0, photo.Length);
                PassList.Add(blog);
            }
            return View(PassList);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Id,Title,Description,Photo")] Blog blog)
        {
            Entity obj = new Entity();
            obj.Id = blog.Id;
            obj.Title = blog.Title;
            obj.Description = blog.Description;
            byte[] file;
            using (var ms = new MemoryStream())
            {
                blog.Photo.CopyTo(ms);
                file = ms.ToArray();
                long length = file.Length;
            }
            if (file.Length < 0)
                return RedirectToAction(actionName: "Index"); 
            _collection.InsertOne(obj);
            bucket.UploadFromBytes(obj.Id, file);
            return RedirectToAction(actionName: "Index");
        }
        public IActionResult Detail(String id)
        {
            var item = _collection.Find(m => m.Id == id).FirstOrDefault();
            var title = item.Title;
            var description = item.Description;
            byte[] photo = bucket.DownloadAsBytesByName(item.Id);
            Pass blog = new Pass();
            blog.Title = title;
            blog.Description = description;
            blog.Photo = Convert.ToBase64String(photo, 0, photo.Length);
            blog.Id=id;
            return View(blog);

        }
        public IActionResult Edit(String id)
        {
            var item = _collection.Find(m => m.Id == id).FirstOrDefault();
            var title = item.Title;
            var description = item.Description;
            byte[] photo = bucket.DownloadAsBytesByName(item.Id);
            Blog blog = new Blog();
            blog.Title = title;
            blog.Description = description;
            TempData["ID"] = id;
            return View(blog);
        }
        public IActionResult SaveAfterEdit([Bind("Id,Title,Description,Photo")] Blog blog)
        {
            string id = (string)TempData["ID"];
            var item = _collection.Find(m => m.Id == id).FirstOrDefault();
            item.Title = blog.Title;
            item.Description = blog.Description;
            byte[] file;
            using (var ms = new MemoryStream())
            {
                blog.Photo.CopyTo(ms);
                file = ms.ToArray();
                long length = file.Length;
            }
            if (file.Length > 0)
            {
                bucket.UploadFromBytes(id, file);
            }
            _collection.ReplaceOne(m=>m.Id == id,item);
            return RedirectToAction(actionName: "List");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
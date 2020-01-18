using iBusinessManagement.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace iBusinessManagement.Controllers
{
    public class CommentController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // id == task id
        [Authorize(Roles = "Administrator,Organizator,User")]
        public ActionResult New(int id)
        {
            var task = db.Tasks.Find(id);
            if (User.IsInRole("Administrator") || User.Identity.GetUserId() == task.Project.OrganizerId ||
                task.Project.Members.Select(m => m.Id).ToList().Contains(User.Identity.GetUserId()))
            {
                var comment = new Comment();
                comment.AuthorId = User.Identity.GetUserId();
                comment.TaskId = id;
                return View(comment);
            }
            else
            {
                TempData["message"] = "Only project members can add comments to tasks";
                return RedirectToAction("Index", "Project");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Organizator,User")]
        public ActionResult New(Comment comment)
        {
            if (ModelState.IsValid)
            {
                var task = db.Tasks.Find(comment.TaskId);
                var author = db.Users.Find(comment.AuthorId);
                comment.Task = task;
                comment.Author = author;
                comment.PostedAt = DateTime.Now;
                db.Comments.Add(comment);
                db.SaveChanges();
                return RedirectToAction("Show", "Job", new { id = comment.TaskId });
            }
            return View(comment);
        }

        // id == comment id
        [Authorize(Roles = "Administrator,Organizator,User")]
        public ActionResult Edit(int id)
        {
            var comment = db.Comments.Find(id);
            if (User.Identity.GetUserId() == comment.AuthorId || User.IsInRole("Administrator"))
            {
                return View(comment);
            }
            TempData["message"] = "No authoriez to edit comment";
            return RedirectToAction("Show", "Job", new { id = comment.TaskId });
        }


        [HttpPut]
        [Authorize(Roles = "Administrator,Organizator,User")]
        public ActionResult Edit(int id, Comment requestComment)
        {
            try
            {

                var comment = db.Comments.Find(id);
                var task = db.Tasks.Find(comment.TaskId);
                if (User.Identity.GetUserId() == comment.AuthorId || User.IsInRole("Administrator"))
                {
                    if (TryUpdateModel(comment))
                    {
                        comment.Message = requestComment.Message;
                        db.SaveChanges();
                    }
                    return RedirectToAction("Show", "Job", new { id = comment.TaskId });
                }
                else
                {
                    TempData["message"] = "Not authorized to edit project";
                    return RedirectToAction("Show", "Job", new { id = comment.TaskId });
                }
            }
            catch (Exception e)
            {
                return View(requestComment);
            }
        }


        [HttpDelete]
        [Authorize(Roles = "Administrator,Organizator,User")]
        public ActionResult Delete(int id)
        {
            Comment comment = db.Comments.Find(id);
            var taskId = comment.TaskId;
            if (User.Identity.GetUserId() == comment.AuthorId || User.IsInRole("Administrator"))
            {
                db.Comments.Remove(comment);
                db.SaveChanges();
                return RedirectToAction("Show", "Job", new { id = taskId });
            }
            else
            {
                TempData["message"] = "Not allowed to delete comment";
                return RedirectToAction("Show", "Job", new { id = taskId });
            }

        }

    }
}
using iBusinessManagement.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace iBusinessManagement.Controllers
{
    public class JobController : Controller
    {
        // GET: Job
        private ApplicationDbContext db = ApplicationDbContext.Create();

        //int id = the id of the event whose tasks we want to view
        [Authorize(Roles = "Administrator,Organizator,User")]
        public ActionResult Index(int id)
        {

            ViewBag.ProjectId = id;
            var project = db.Projects.Find(id);

            ViewBag.Tasks = project.Tasks;
            var projectTitle = db.Projects.Find(id).Title;

            if (project.OrganizerId == User.Identity.GetUserId() || User.IsInRole("Administrator") || project.Members.Select(m => m.Id).ToList().Contains(User.Identity.GetUserId()))
            {
                return View();
            }
            else
            {
                TempData["Message"] = "Not authorized to access this task";
                return RedirectToAction("Index");
            }
        }


        [Authorize(Roles = "Administrator,Organizator")]
        public ActionResult New(int id)
        {
            Job task = new Job();
            task.ProjectId = id;
            return View(task);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Organizator")]
        public ActionResult New(Job task)
        {
            task.Project = db.Projects.Find(task.ProjectId);
            task.StartDate = DateTime.Now;
            task.Status = "Unassigned";

            try
            {
                if (ModelState.IsValid)
                {
                    db.Tasks.Add(task);
                    db.SaveChanges();
                    TempData["message"] = "Task created";
                    return RedirectToAction("Show", "Project", new { id = task.ProjectId });

                }
                else
                {
                    return View(task);
                }
            }
            catch (Exception e)
            {
                return View(task);
            }
        }

        public ActionResult Show(int id)
        {
            var task = db.Tasks.Find(id);
            ViewBag.ProjectTitle = task.Project.Title;
            ViewBag.ProjectId = task.Project.Id;
            return View(task);
        }

        //id = task id
        [Authorize(Roles = "Administrator,Organizator")]
        public ActionResult Edit(int id)
        {
            Job task = db.Tasks.Find(id);
            if (task.Project.OrganizerId == User.Identity.GetUserId() || User.IsInRole("Administrator"))
            {
                return View(task);
            }
            else
            {
                TempData["message"] = "Not authorized to modify project";
                return RedirectToAction("Index");
            }
        }

        [HttpPut]
        [Authorize(Roles = "Administrator,Organizator")]
        public ActionResult Edit(int id, Job requestTask)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Job task = db.Tasks.Find(id);
                    if (task.Project.OrganizerId == User.Identity.GetUserId() || User.IsInRole("Administrator"))
                    {
                        if (TryUpdateModel(task))
                        {
                            task.Title = requestTask.Title;
                            task.Description = requestTask.Description;
                            task.Deadline = requestTask.Deadline;
                            db.SaveChanges();
                            TempData["message"] = "Task edited successfuly";
                        }

                        return RedirectToAction("Show", "Project", new { id = task.Project.Id });
                    }
                    else
                    {
                        TempData["message"] = "Not authorized to edit tasks";
                        return RedirectToAction("Show", "Project", new { id = task.Project.Id });
                    }
                }
                else
                {
                    return View();
                }

            }
            catch (Exception e)
            {
                return View(requestTask);
            }
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator,Organizator")]
        public ActionResult Delete(int id)
        {
            var task = db.Tasks.Find(id);
            var projectId = task.Project.Id;
            if (task.Project.OrganizerId == User.Identity.GetUserId() || User.IsInRole("Administrator"))
            {
                db.Tasks.Remove(task);
                db.SaveChanges();
                TempData["message"] = "Task deleted";
            }
            else
            {
                TempData["message"] = "Not authorized to modify project";
            }
            return RedirectToAction("Show", "Project", new { id = projectId });
        }

        [Authorize(Roles = "Administrator,Organizator")]
        public ActionResult Assign(int id)
        {
            var task = db.Tasks.Find(id);

            var members = task.Project.Members.Select(x =>
                                new SelectListItem
                                {
                                    Value = x.Id,
                                    Text = x.UserName
                                });

            var membersSelect = new SelectList(members, "Value", "Text");

            var model = new AssignJobModel
            {
                TaskId = id,
                MembersSelect = membersSelect
            };
            return View(model);
        }

        [HttpPut]
        [Authorize(Roles = "Administrator,Organizator")]
        public ActionResult Assign(AssignJobModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var task = db.Tasks.Find(model.TaskId);
                    var assignee = db.Users.Find(model.AssigneeId);

                    if (task.Project.OrganizerId == User.Identity.GetUserId() || User.IsInRole("Administrator"))
                    {
                        if (TryUpdateModel(task))
                        {
                            task.AssigneeID = assignee.Id;
                            task.Assignee = assignee;
                            task.Status = "Assigned";
                            db.SaveChanges();
                            TempData["message"] = "Assigned successfuly";
                        }
                        return RedirectToAction("Show", new { id = model.TaskId });
                    }
                    else
                    {
                        TempData["message"] = "Not authorized to assign tasks for this project";
                        return RedirectToAction("Index", new { id = task.Project.Id });
                    }
                }
                else
                {
                    return View(model);
                }
            }
            catch (Exception e)
            {
                return View(model);
            }
        }

        [Authorize(Roles = "Administrator,Organizator,User")]
        public ActionResult UpdateStatus(int id)
        {
            var task = db.Tasks.Find(id);
            var userId = User.Identity.GetUserId();
            List<SelectListItem> listItems = new List<SelectListItem>();
            listItems.Add(new SelectListItem
            {
                Text = "In Progress",
                Value = "In Progress"
            });
            listItems.Add(new SelectListItem
            {
                Text = "In Review",
                Value = "In Review"
            });
            listItems.Add(new SelectListItem
            {
                Text = "In Deployment",
                Value = "In Deployment"
            });
            listItems.Add(new SelectListItem
            {
                Text = "Done",
                Value = "Done"
            });
            var taskStatus = new StatusJobModel
            {
                TaskId = task.Id,
                Statuses = listItems
            };
            if (User.IsInRole("Administrator") || task.AssigneeID == User.Identity.GetUserId())
            {
                return View(taskStatus);
            }
            else
            {
                return RedirectToAction("Index", "Project");
            }
        }

        [HttpPut]
        [Authorize(Roles = "Administrator,Organizator,User")]
        public ActionResult UpdateStatus(StatusJobModel statusRequest)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var task = db.Tasks.Find(statusRequest.TaskId);

                    if (task.AssigneeID == User.Identity.GetUserId() || User.IsInRole("Administrator"))
                    {
                        if (TryUpdateModel(task))
                        {
                            task.Status = statusRequest.Status;
                            db.SaveChanges();
                            TempData["message"] = "Status Updated";
                        }
                        return RedirectToAction("Show", new { id = statusRequest.TaskId });
                    }
                    else
                    {
                        TempData["message"] = "Not authorized to update tasks for this project";
                        return RedirectToAction("Index", new { id = task.Project.Id });
                    }
                }
                else
                {
                    return View(statusRequest);
                }
            }
            catch (Exception e)
            {
                return View(statusRequest);
            }
        }

    }
}
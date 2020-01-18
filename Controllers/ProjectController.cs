using iBusinessManagement.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace iBusinessManagement.Controllers
{
    public class ProjectController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Project
        [Authorize(Roles = "User,Organizator,Administrator")]
        public ActionResult Index()
        {
            ViewBag.UserId = User.Identity.GetUserId();

            var list = db.Projects.ToList().Where(p => p.Members.Select(m => m.Id).ToList().Contains(ViewBag.UserId) || p.OrganizerId == ViewBag.UserId || User.IsInRole("Administrator"));

            ViewBag.Projects = list;

            list = list.ToList();

            ViewBag.ProjectsIsEmpty = list.Any();
            return View();
        }


        [Authorize(Roles = "User,Organizator,Administrator")]
        public ActionResult Show(int id)
        {
            ViewBag.CurrentUserId = User.Identity.GetUserId();
            var project = db.Projects.Find(id);
            return View(project);
        }
        

        [Authorize(Roles = "User,Organizator,Administrator")]
        public ActionResult New()
        {
            Project project = new Project
            {
                OrganizerId = User.Identity.GetUserId()
            };
            return View(project);
        }



        [HttpPost]
        [Authorize(Roles = "User,Organizator,Administrator")]
        public ActionResult New(Project project)
        {
            project.OrganizerId = User.Identity.GetUserId();

            project.Organizer = db.Users.Find(project.OrganizerId);

            var UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

            var user = db.Users.Find(User.Identity.GetUserId());

            try
            {
                if (ModelState.IsValid)
                {
                    db.Projects.Add(project);
                    TempData["message"] = "Proiectul a fost creat";
                    db.SaveChanges();

                    if (User.IsInRole("User"))
                    {
                        UserManager.RemoveFromRole(user.Id, "User");
                        UserManager.AddToRole(user.Id, "Organizator");
                        db.SaveChanges();
                    }

                    return RedirectToAction("Index");
                }
                else
                {
                    return View(project);
                }
            }
            catch (Exception e)
            {
                return View(project);
            }
        }


        [Authorize(Roles = "Administrator,Organizator")]
        public ActionResult Edit(int id)
        {
            Project project = db.Projects.Find(id);

            if (project.OrganizerId == User.Identity.GetUserId() || User.IsInRole("Administrator"))
            {
                return View(project);
            }
            else
            {
                TempData["message"] = "Not authorized to modify this project";
                return RedirectToAction("Index");
            }
        }

        [HttpPut]
        [Authorize(Roles = "Administrator,Organizator")]
        public ActionResult Edit(int id, Project requestProject)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Project project = db.Projects.Find(id);

                    if (project.OrganizerId == User.Identity.GetUserId() || User.IsInRole("Administrator"))
                    {
                        if (TryUpdateModel(project))
                        {
                            project.Title = requestProject.Title;
                            project.Description = requestProject.Description;
                            db.SaveChanges();
                            TempData["message"] = "Project has been edited";
                        }
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["message"] = "Not authorized to edit this project";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    return View();
                }
            }
            catch (Exception e)
            {
                return View();
            }
        }


        [HttpDelete]
        [Authorize(Roles = "Administrator,Organizator")]
        public ActionResult Delete(int id)
        {
            Project project = db.Projects.Find(id);

            if (project.OrganizerId == User.Identity.GetUserId() || User.IsInRole("Administrator"))
            {
                db.Projects.Remove(project);
                db.SaveChanges();
                TempData["message"] = "The project has been deleted";
            }
            else
            {
                TempData["message"] = "Not authorized to delete this project";
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Administrator,Organizator")]
        public ActionResult AddMember(int id)
        {

            var model = new ProjectMembersModel
            {
                ProjectId = id,
                UsersSelect = GetUsers(id)
            };
            return View(model);
        }

        [HttpPut]
        [Authorize(Roles = "Administrator,Organizator")]
        public ActionResult AddMember(ProjectMembersModel projectUser)
        {

            try
            {
                if (ModelState.IsValid)
                {
                    var project = db.Projects.Find(projectUser.ProjectId);
                    var member = db.Users.Find(projectUser.SelectedUserId);

                    if (project.OrganizerId == User.Identity.GetUserId() || User.IsInRole("Administrator"))
                    {
                        if (TryUpdateModel(project))
                        {
                            project.Members.Add(member);
                            db.SaveChanges();
                            TempData["message"] = "Member added successfuly";
                        }
                        return RedirectToAction("Show", new { id = projectUser.ProjectId });
                    }
                    else
                    {
                        TempData["message"] = "Not authorized to add members to project";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    return View(projectUser);
                }
            }
            catch (Exception e)
            {
                return View(projectUser);
            }
        }

        private IEnumerable<SelectListItem> GetUsers(int projectId)
        {
            var project = db.Projects.Find(projectId);
            var users = db.Users.ToList().Except(project.Members.ToList()).ToList();
            var usersSelect = users.Select(x =>
                                new SelectListItem
                                {
                                    Value = x.Id,
                                    Text = x.UserName
                                });

            return new SelectList(usersSelect, "Value", "Text");
        }

    }
}
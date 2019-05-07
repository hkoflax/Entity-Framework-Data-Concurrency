using ComicBookShared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using ComicBookLibraryManagerWebApp.ViewModels;
using System.Net;
using System.Data.Entity.Infrastructure;
using ComicBookShared.Data;
using ComicBookLibraryManagerWebApp.Controllers;
using ComicBookLibraryManagerWebApp.ViewModels;

namespace ComicBookLibraryManagerWebApp.Controllers
{
    /// <summary>
    /// Controller for the "Comic Books" section of the website.
    /// </summary>
    public class ComicBooksController : BaseController
    {
        private ComicBookRepository _comicBookRepository = null;
        private SeriesRepository _seriesRepository = null;
        private ArtistRepository _artistRepository = null;
        public ComicBooksController()
        {
            _comicBookRepository = new ComicBookRepository(Context);
            _seriesRepository = new SeriesRepository(Context);
            _artistRepository = new ArtistRepository(Context);
        }
        public ActionResult Index()
        {
            // TODO Get the comic books list.
            // Include the "Series" navigation property.
            // we copied the query from repository and add it here using the  new context
            var comicBooks = _comicBookRepository.GetList();

            return View(comicBooks);
        }

        public ActionResult Detail(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // TODO Get the comic book.
            // Include the "Series", "Artists.Artist", and "Artists.Role" navigation properties.
            var comicBook = _comicBookRepository.Get((int)id);

            if (comicBook == null)
            {
                return HttpNotFound();
            }

            // Sort the artists.
            comicBook.Artists = comicBook.Artists.OrderBy(a => a.Role.Name).ToList();

            return View(comicBook);
        }

        public ActionResult Add()
        {
            var viewModel = new ComicBooksAddViewModel();

            // TODO Pass the Context class to the view model "Init" method.
            viewModel.Init(Repository,_seriesRepository,_artistRepository);

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult Add(ComicBooksAddViewModel viewModel)
        {
            ValidateComicBook(viewModel.ComicBook);

            if (ModelState.IsValid)
            {
                var comicBook = viewModel.ComicBook;
                comicBook.AddArtist(viewModel.ArtistId, viewModel.RoleId);

                // TODO Add the comic book.
                _comicBookRepository.Add(comicBook);

                TempData["Message"] = "Your comic book was successfully added!";

                return RedirectToAction("Detail", new { id = comicBook.Id });
            }

            // TODO Pass the Context class to the view model "Init" method.
            viewModel.Init(Repository,_seriesRepository,_artistRepository);

            return View(viewModel);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // TODO Get the comic book.
            var comicBook = _comicBookRepository.Get((int) id, 
                includeRelatedEntities:false);

            if (comicBook == null)
            {
                return HttpNotFound();
            }
            
            var viewModel = new ComicBooksEditViewModel()
            {
                ComicBook = comicBook
            };
            viewModel.Init(Repository,_seriesRepository,_artistRepository);

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult Edit(ComicBooksEditViewModel viewModel)
        {
            ValidateComicBook(viewModel.ComicBook);

            if (ModelState.IsValid)
            {
                var comicBook = viewModel.ComicBook;

                // TODO Update the comic book.
                try
                {
                    _comicBookRepository.Update(comicBook);

                    TempData["Message"] = "Your comic book was successfully updated!";

                    return RedirectToAction("Detail", new { id = comicBook.Id });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    string message = null;

                    var entitypropertyValues = ex.Entries.Single().GetDatabaseValues();
                    if (entitypropertyValues==null)
                    {
                        message = "The comic  book being updated has been deleted by another user. " +
                            "Click 'cancel' button to return to list page";
                        viewModel.ComicBookHasBeenDeleted=true;
                    }
                    else
                    {
                        message = "The comic book being updated has already been updated by another user. If you still " +
                            "want to make your changes the click the 'Save' button again. Otherwise click the 'Cancel' button " +
                            "to discard your changes";

                        comicBook.RowVersion = ((ComicBook)entitypropertyValues.ToObject()).RowVersion;
                    }

                    ModelState.AddModelError(string.Empty, message);
                }
            }

            viewModel.Init(Repository,_seriesRepository,_artistRepository);

            return View(viewModel);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // TODO Get the comic book.
            // Include the "Series" navigation property.
            var comicBook = _comicBookRepository.Get((int)id);

            if (comicBook == null)
            {
                return HttpNotFound();
            }

            var viewModel = new ComicBooksDeleteViewModel()
            {
                ComicBook = comicBook
            };
            return View(viewModel);
        }

        [HttpPost]
        public ActionResult Delete(ComicBooksDeleteViewModel viewModel)
        {
            try
            {
                // TODO Delete the comic book.
                _comicBookRepository.Delete(viewModel.ComicBook.Id, viewModel.ComicBook.RowVersion);

                TempData["Message"] = "Your comic book was successfully deleted!";

                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                string message = null;

                var entitypropertyValues = ex.Entries.Single().GetDatabaseValues();
                if (entitypropertyValues == null)
                {
                    message = "The comic  book being deleted has been deleted by another user. " +
                        "Click 'cancel' button to return to list page";
                    viewModel.ComicBookHasBeenDeleted = true;
                }
                else
                {
                    message = "The comic book being deleted has already been updated by another user. If you still " +
                        "want to delete the comic then click the 'Delete' button again. Otherwise click the 'Cancel' button " +
                        "to return to detail page";

                    viewModel.ComicBook.RowVersion = ((ComicBook)entitypropertyValues.ToObject()).RowVersion;
                }

                ModelState.AddModelError(string.Empty, message);
                return View(viewModel);
            }
        }

        /// <summary>
        /// Validates a comic book on the server
        /// before adding a new record or updating an existing record.
        /// </summary>
        /// <param name="comicBook">The comic book to validate.</param>
        private void ValidateComicBook(ComicBook comicBook)
        {
            // If there aren't any "SeriesId" and "IssueNumber" field validation errors...
            if (ModelState.IsValidField("ComicBook.SeriesId") &&
                ModelState.IsValidField("ComicBook.IssueNumber"))
            {
                // Then make sure that the provided issue number is unique for the provided series.
                // TODO Call method to check if the issue number is available for this comic book.
                if (_comicBookRepository.ComicBookSerieshasissueNumber(
                    comicBook.Id,comicBook.SeriesId,comicBook.IssueNumber))
                {
                    ModelState.AddModelError("ComicBook.IssueNumber",
                        "The provided Issue Number has already been entered for the selected Series.");
                }
            }
        }
    }
}
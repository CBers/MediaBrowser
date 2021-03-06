﻿using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using ServiceStack.ServiceHost;
using System;
using System.Linq;

namespace MediaBrowser.Api
{
    [Route("/Albums/{Id}/Similar", "GET")]
    [Api(Description = "Finds albums similar to a given album.")]
    public class GetSimilarAlbums : BaseGetSimilarItemsFromItem
    {
    }

    public class AlbumsService : BaseApiService
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The _user data repository
        /// </summary>
        private readonly IUserDataRepository _userDataRepository;
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly IItemRepository _itemRepo;

        public AlbumsService(IUserManager userManager, IUserDataRepository userDataRepository, ILibraryManager libraryManager, IItemRepository itemRepo)
        {
            _userManager = userManager;
            _userDataRepository = userDataRepository;
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSimilarAlbums request)
        {
            var result = SimilarItemsHelper.GetSimilarItemsResult(_userManager,
                _itemRepo,
                _libraryManager,
                _userDataRepository,
                Logger,
                request, item => item is MusicAlbum,
                GetAlbumSimilarityScore);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the album similarity score.
        /// </summary>
        /// <param name="item1">The item1.</param>
        /// <param name="item2">The item2.</param>
        /// <returns>System.Int32.</returns>
        private int GetAlbumSimilarityScore(BaseItem item1, BaseItem item2)
        {
            var points = SimilarItemsHelper.GetSimiliarityScore(item1, item2);

            var album1 = (MusicAlbum)item1;
            var album2 = (MusicAlbum)item2;

            var artists1 = album1.RecursiveChildren
                .OfType<Audio>()
                .SelectMany(i => new[] { i.AlbumArtist, i.Artist })
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var artists2 = album2.RecursiveChildren
                .OfType<Audio>()
                .SelectMany(i => new[] { i.AlbumArtist, i.Artist })
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            return points + artists1.Where(artists2.ContainsKey).Sum(i => 5);
        }
    }
}

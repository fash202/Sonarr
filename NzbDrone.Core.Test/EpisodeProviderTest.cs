﻿// ReSharper disable RedundantUsingDirective
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using AutoMoq;
using FizzWare.NBuilder;
using MbUnit.Framework;
using Moq;
using NzbDrone.Core.Model;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Repository.Quality;
using NzbDrone.Core.Test.Framework;
using SubSonic.Repository;
using TvdbLib.Data;

namespace NzbDrone.Core.Test
{
    [TestFixture]
    // ReSharper disable InconsistentNaming
    public class EpisodeProviderTest : TestBase
    {
        [Test]
        public void RefreshEpisodeInfo()
        {
            //Arrange
            const int seriesId = 71663;
            const int episodeCount = 10;

            var fakeEpisodes = Builder<TvdbSeries>.CreateNew().With(
                c => c.Episodes =
                     new List<TvdbEpisode>(Builder<TvdbEpisode>.CreateListOfSize(episodeCount).
                                               WhereAll()
                                               .Have(l => l.Language = new TvdbLanguage(0, "eng", "a"))
                                               .Build())
                ).With(c => c.Id = seriesId).Build();

            var mocker = new AutoMoqer();

            mocker.SetConstant(MockLib.GetEmptyRepository());

            mocker.GetMock<TvDbProvider>()
                .Setup(c => c.GetSeries(seriesId, true))
                .Returns(fakeEpisodes).Verifiable();

            //mocker.GetMock<IRepository>().SetReturnsDefault();


            //Act
            var sw = Stopwatch.StartNew();
            mocker.Resolve<EpisodeProvider>().RefreshEpisodeInfo(seriesId);
            var actualCount = mocker.Resolve<EpisodeProvider>().GetEpisodeBySeries(seriesId);
            //Assert
            mocker.GetMock<TvDbProvider>().VerifyAll();
            Assert.Count(episodeCount, actualCount);
            Console.WriteLine("Duration: " + sw.Elapsed);
        }

        [Test]

        //Should Download
        [Row(QualityTypes.TV, true, QualityTypes.HDTV, false, true)]
        [Row(QualityTypes.DVD, true, QualityTypes.Bluray720, true, true)]
        [Row(QualityTypes.HDTV, false, QualityTypes.HDTV, true, true)]


        [Row(QualityTypes.HDTV, false, QualityTypes.HDTV, false, false)]
        [Row(QualityTypes.Bluray720, true, QualityTypes.Bluray1080, false, false)]
        [Row(QualityTypes.HDTV, true, QualityTypes.Bluray720, true, false)]
        [Row(QualityTypes.Bluray1080, true, QualityTypes.Bluray720, true, false)]
        [Row(QualityTypes.Bluray1080, false, QualityTypes.Bluray720, true, false)]
        [Row(QualityTypes.Bluray1080, false, QualityTypes.Bluray720, true, false)]
        [Row(QualityTypes.HDTV, false, QualityTypes.Bluray720, true, false)]
        public void Is_Needed_Tv_Dvd_BluRay_BluRay720_Is_Cutoff(QualityTypes fileQuality, bool isFileProper, QualityTypes reportQuality, bool isReportProper, bool excpected)
        {
            //Setup
            var parseResult = new EpisodeParseResult
                                  {
                                      SeasonNumber = 2,
                                      Episodes = new List<int> { 3 },
                                      Quality = reportQuality,
                                      Proper = isReportProper
                                  };

            var epFile = new EpisodeFile
                             {
                                 Proper = isFileProper,
                                 Quality = fileQuality
                             };

            var seriesQualityProfile = new QualityProfile
            {
                Name = "HD",
                Allowed = new List<QualityTypes> { QualityTypes.HDTV, QualityTypes.WEBDL, QualityTypes.BDRip, QualityTypes.Bluray720 },
                Cutoff = QualityTypes.HDTV
            };

            var episodeInfo = new Episode
                                  {
                                      SeriesId = 12,
                                      SeasonNumber = 2,
                                      EpisodeNumber = 3,
                                      Series = new Series { QualityProfileId = 1, QualityProfile = seriesQualityProfile },
                                      EpisodeFile = epFile

                                  };

            var mocker = new AutoMoqer();

            var result = mocker.Resolve<EpisodeProvider>().IsNeeded(parseResult, episodeInfo);

            Assert.AreEqual(excpected, result);
        }

        [Test]
        [Explicit]
        public void Add_daily_show_episodes()
        {
            var mocker = new AutoMoqer();
            mocker.SetConstant(MockLib.GetEmptyRepository());
            mocker.Resolve<TvDbProvider>();
            const int tvDbSeriesId = 71256;
            //act
            var seriesProvider = mocker.Resolve<SeriesProvider>();

            seriesProvider.AddSeries("c:\\test\\", tvDbSeriesId, 0);
            var episodeProvider = mocker.Resolve<EpisodeProvider>();
            episodeProvider.RefreshEpisodeInfo(tvDbSeriesId);

            //assert
            var episodes = episodeProvider.GetEpisodeBySeries(tvDbSeriesId);
            Assert.IsNotEmpty(episodes);
        }



    }
}                                                                                                                                                                                                                                                                                                                                                                             
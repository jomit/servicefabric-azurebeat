(function (AZBEAT) {
    "use strict";

    AZBEAT.homeService = function ($http, $q, coreService) {

        function getAllTopics() {
            var deferred = $q.defer();
            coreService.callService({
                api: "api/v1/topic/getall",
                method: "GET"
            })
            .then(function (response) {
                deferred.resolve(response.data);
            });
            return deferred.promise;
        }

        function getAllFeeds() {
            var deferred = $q.defer();
            coreService.callService({
                api: "api/v1/feed/getall",
                method: "GET"
            })
            .then(function (response) {
                deferred.resolve(response.data);
            });
            return deferred.promise;
        }

        function addTopic(topicName) {
            var deferred = $q.defer();
            coreService.callService({
                api: "api/v1/topic/create",
                method: "POST",
                data: { "name": topicName }
            })
            .then(function (response) {
                deferred.resolve(response.data);
            });
            return deferred.promise;
        }

        function addFeed(feedItem) {
            var deferred = $q.defer();
            coreService.callService({
                api: "api/v1/feed/create",
                method: "POST",
                data: feedItem
            })
            .then(function (response) {
                deferred.resolve(response.data);
            });
            return deferred.promise;
        }

        function getAllSubscriptions() {
            var deferred = $q.defer();
            coreService.callService({
                api: "api/v1/subscription/getall",
                method: "GET"
            })
            .then(function (response) {
                deferred.resolve(response.data);
            });
            return deferred.promise;
        }


        return {
            getAllTopics: getAllTopics,
            getAllFeeds: getAllFeeds,
            getAllSubscriptions: getAllSubscriptions,
            addTopic: addTopic,
            addFeed: addFeed
        };
    };

    AZBEAT.homeController = function ($timeout, homeService, coreService) {
        var home = this;
        home.currentFeed = null;
        home.currentSubscription = null;
        home.topicName = "";
        home.newFeed = {
            url: ""
        };
        home.initialize = function () {
            home.getAllTopics();
            if (location.href.endsWith("managefeeds.html"))
                home.getAllFeeds();

            if (location.href.endsWith("viewsubscriptions.html")) {
                home.getAllSubscriptions();
            }
        };

        home.getAllTopics = function () {
            homeService.getAllTopics()
               .then(function (data) {
                   home.topics = data;
               });
        };

        home.getAllFeeds = function () {
            homeService.getAllFeeds()
               .then(function (data) {
                   home.feeds = data;
               });
        };

        home.addTopic = function () {
            homeService.addTopic(home.topicName)
              .then(function (data) {
                  home.topics.push({ "Name": home.topicName });
                  home.topicName = "";
              });
        };

        home.addFeed = function () {
            homeService.addFeed(home.newFeed)
              .then(function (data) {
                  home.feeds.push({ "Url": home.newFeed.url });
                  home.newFeed.url = "";
              });
        };

        home.setCurrentFeed = function (selectedFeed) {
            home.currentFeed = selectedFeed;
        }

        home.setCurrentSubscription = function (selectedSubscription) {
            home.currentSubscription = selectedSubscription;
        }

        home.getAllSubscriptions = function () {
            homeService.getAllSubscriptions()
               .then(function (data) {
                   home.subscriptions = data;
               });
        };

        home.initialize();
    };

    angular.module("home", [])
       .service('coreService', AZBEAT.coreService)
       .service('homeService', AZBEAT.homeService)
       .controller('homeController', AZBEAT.homeController);

    if (document.getElementById("homePage")) {
        angular.bootstrap(document, ['home']);
    }
})(AZBEAT);
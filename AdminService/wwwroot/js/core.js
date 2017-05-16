var AZBEAT = AZBEAT || {};


(function (AZBEAT) {
    "use strict";

    AZBEAT.cacheService = (function () {

        var defaultMinutes = 2;

        function getCacheData(key) {
            try {
                var data = JSON.parse(localStorage.getItem(key));
                if (null === data || data.expiration === undefined)
                    return null;
                //logError("Expiration Date =>" + data.expiration);
                //logError("Getting cached value for => " + key + " with expiration" + new Date(data.expiration).toString());
                if (new Date(data.expiration) <= new Date()) {
                    //logError("Removing cached value for => " + key);
                    removeCacheData(key);
                    return null;
                } else {
                    return JSON.parse(localStorage.getItem(key)).data;
                }
            }
            catch (err) {
                console.log("Error in getCacheData => " + key);
            }
        }

        function setCacheData(key, value, expirationMinutes) {
            try {
                var currentDate = new Date();
                if (!expirationMinutes)
                    expirationMinutes = defaultMinutes;

                currentDate.setMinutes(currentDate.getMinutes() + expirationMinutes);
                var newValue = {
                    data: value,
                    expiration: currentDate
                };
                //logError("Adding cached value for => " + key + " with expiration " + newValue.expiration);
                localStorage.setItem(key, JSON.stringify(newValue));
            }
            catch (err) { console.log("Error in setCacheData => " + key); }
        }

        function removeCacheData(key) {
            localStorage.removeItem(key);
        }

        function clearAllCache() {
            localStorage.clear();
        }

        return {
            getCacheData: getCacheData,
            setCacheData: setCacheData,
            removeCacheData: removeCacheData,
            clearAllCache: clearAllCache
        };
    })();

    AZBEAT.coreService = function ($http, $q) {
        var baseUrl = location.origin + "/";

        function sort(propertyName, sortDescending) {
            var sortOrder = 1;
            if (sortDescending === true) {
                sortOrder = -1;
            }
            return function (a, b) {
                var data = (a[propertyName] < b[propertyName]) ? -1 : (a[propertyName] > b[propertyName]) ? 1 : 0;
                return data * sortOrder;
            };
        }

        function log(message) {
            console.log(message);
        }

        function groupBy(list, fn) {
            var groups = {};
            for (var i = 0; i < list.length; i++) {
                var group = fn(list[i]); // JSON.stringify(fn(list[i]));  //for multiple group by fields using stringify
                if (group[0] === null) group = -1;
                if (group in groups) {
                    groups[group].push(list[i]);
                } else {
                    groups[group] = [list[i]];
                }
            }
            return groups; //arrayFromObject(groups);
        }

        function callService(options) {
            var deferred = $q.defer();
            var cacheService = AZBEAT.cacheService;
            if (options.cacheKey) {
                var cacheData = cacheService.getCacheData(options.cacheKey);
                if (null !== cacheData) {
                    deferred.resolve(cacheData);
                    return deferred.promise;
                }
            }
            $http({
                url: baseUrl + options.api,
                method: options.method,
                data: JSON.stringify(options.data),
                headers: { 'Content-Type': 'application/json'},
            })
            .then(function (response) {
                if (options.cacheKey) {
                    cacheService.setCacheData(options.cacheKey, response);
                }
                deferred.resolve(response);
            }, function (error) {
                log("Error while calling " + options.url);
            });
            return deferred.promise;
        }

        function getQueryString(name, defaultValue) {
            var query = window.location.search.substring(1);
            var vars = query.split("&");
            for (var i = 0; i < vars.length; i++) {
                var pair = vars[i].split("=");
                if (pair[0].toLowerCase() == name.toLowerCase()) { return pair[1]; }
            }
            if (defaultValue)
                return defaultValue;

            return -1;
        }

        return {
            sort: sort,
            groupBy: groupBy,
            log: log,
            getQueryString: getQueryString,
            callService: callService
        };
    };
})(AZBEAT);
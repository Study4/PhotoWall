angular.module('photoWall.services.photoServices', [
    'ngResource'
]).factory('photoServices', function ($resource) {
    return $resource('/api/photo/:Id', { Id: '@id' }, {
            query: { method: 'GET', isArray: true },
            queryByID: { method: 'GET' }
        });
    });
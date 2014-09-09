angular.module("photoWall.controllers.photoWallController", [
    "photoWall.services.photoServices",
    'bootstrapLightbox',
    'SignalR'
]).controller("photoWallController", [
    "$scope", "photoServices", "Lightbox",'Hub',
    function ($scope, photoServices, Lightbox, Hub) {
        $scope.isUploadNow = false;
        $scope.photos = [];

        //Get All
        photoServices.query(function (data) {
            for (var i = 0; i < data.length; i++) {
                data[i].url = data[i].imageURL;
                $scope.photos = data;
            }
        });

        //openLightbox
        $scope.openLightboxModal = function (index) {
            Lightbox.openModal($scope.photos, index);
        };

        var hub = new Hub('photoHub', {

            //client side methods
            listeners: {
                'sendAllMessge': function (data) {
                    $scope.photos.unshift({
                        id:data.Id,
                        uploadUser:data.UploadUser,
                        description:data.Description,
                        imageURL:data.ImageURL,
                        thumbnailURL:data.ThumbnailURL,
                        uploadDate:data.Upload
                    });
                    $scope.$apply();
                }
            },

            //handle connection error
            errorHandler: function (error) {
                console.error(error);
            }

        });
    }]);
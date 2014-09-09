angular.module("photoWall.controllers.photoWallController", [
    "photoWall.common.fileInput",
    "photoWall.services.photoServices",
    'bootstrapLightbox',
    'SignalR'
]).controller("photoWallController", [
    "$scope", "$http", "photoServices", "Lightbox",'Hub',
    function ($scope, $http, photoServices, Lightbox, Hub) {
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

        $scope.fileChanged = function (elm) {
            $scope.files = elm.files;
            $scope.$apply();
        };

        $scope.upload = function () {
            if ($scope.files == undefined) { return; }
            $scope.isUploadNow = true;
            var fd = new FormData();
            angular.forEach($scope.files, function (file) {
                fd.append("file", file);
            });

            $http.post("/api/Photo", fd, {
                transformRequest: angular.identity,
                headers: { "Content-Type": undefined }
            }).success(function (d) {
                $scope.isUploadNow = false;
                alert("已送出");
                console.log(d);
            });
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
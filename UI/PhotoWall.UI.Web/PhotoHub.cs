using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using PhotoWall.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace PhotoWall.UI.Web
{
    public class PhotoHub : Hub
    {
        public void Send(Photo value)
        {
            Clients.All.sendAllMessge(value);
        }
    }
}
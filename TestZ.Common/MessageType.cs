using System;
using System.Collections.Generic;
using System.Text;

namespace TestZ.Common
{
    /// <summary>
    /// Protocol messgaes types. 
    ///
    /// Directions:
    ///   C->S: client to server;
    ///   S->C: server to client.
    /// </summary>
    public enum MessageType : byte
    {
        /// <summary>c->s. Client registrations with metadata like username, domen etc.The first message after log on</summary>
        Hello = 1,

        /// <summary>s->c. Server response to Hello-message, sessions params</summary>
        Welcome = 2,

        /// <summary>c->s. clients pulse: keep-alive + user activity data</summary>
        Heartbeat = 3,

        /// <summary>s->c. server requests screenshot, payload is empty</summary>
        ScreenshotRequest = 4,

        /// <summary>c->s. jpeg bytes</summary>
        ScreenshotData = 5,
    }

}

/*
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2008-2009 Thomas Bruderer <apophis@apophis.ch> <http://www.apophis.ch>
 *
 * Full LGPL License: <http://www.gnu.org/licenses/lgpl.txt>
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;
using System.Collections.Generic;

namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// Dcc Event Args Involving Lines of Text
    /// </summary>
    public class DccChatEventArgs : DccEventArgs
    {
        internal DccChatEventArgs(DccConnection dcc, string messageLine) : base(dcc)
        {
            // = { ' ' };
            this.Message = messageLine;
            this.MessageArray = messageLine.Split(new char[] { ' ' });
        }

        public string Message { get; }

        public string[] MessageArray { get; }
    }

    /// <summary>
    /// Base DCC Event Arguments
    /// </summary>
    public class DccEventArgs : EventArgs
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="dccClient"></param>
        /// <param name="stream">If there are multiple streams on a DCC (a channel DCC) this identifies the stream</param>
        internal DccEventArgs(DccConnection dcc) => this.Dcc = dcc;

        public DccConnection Dcc { get; }
    }

    /// <summary>
    /// Dcc Event Args involving Packets of Bytes
    /// </summary>
    public class DccSendEventArgs : DccEventArgs
    {
        internal DccSendEventArgs(DccConnection dcc, byte[] package, int packageSize) : base(dcc)
        {
            this.Package = package;
            this.PackageSize = packageSize;
        }

        public byte[] Package { get; }

        public int PackageSize { get; }
    }

    /// <summary>
    /// Special DCC Event Arg for Receiving File Requests
    /// </summary>
    public class DccSendRequestEventArgs : DccEventArgs
    {
        internal DccSendRequestEventArgs(DccConnection dcc, string filename, long filesize) : base(dcc)
        {
            this.Filename = filename;
            this.Filesize = filesize;
        }

        public string Filename { get; }

        public long Filesize { get; }
    }
}
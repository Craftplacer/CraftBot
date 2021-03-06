// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2014 Mirco Bauer <meebey@meebey.net>
//
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
using System;
using System.Linq;
using System.Collections.Generic;

namespace Meebey.SmartIrc4net
{
    public enum ChannelMode
    {
        Unknown,
        Op = 'o',
        Owner = 'q',
        Admin = 'a',
        HalfOp = 'h',
        Voice = 'v',
        Ban = 'b',
        BanException = 'e',
        InviteException = 'I',
        Key = 'k',
        UserLimit = 'l',
        TopicLock = 't'
    }

    public enum ChannelModeChangeAction
    {
        Set,
        Unset
    }

    public enum ChannelModeHasParameter
    {
        Always,
        OnlySet,
        Never
    }

    public class ChannelModeChangeInfo
    {
        public ChannelModeChangeInfo()
        {
        }

        public ChannelModeChangeAction Action { get; private set; }
        public ChannelMode Mode { get; private set; }
        public char ModeChar { get; private set; }
        public string Parameter { get; private set; }

        public static List<ChannelModeChangeInfo> Parse(ChannelModeMap modeMap, string target, string mode, string modeParameters)
        {
            if (modeMap == null)
            {
                throw new ArgumentNullException("modeMap");
            }
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            if (mode == null)
            {
                throw new ArgumentNullException("mode");
            }
            if (modeParameters == null)
            {
                throw new ArgumentNullException("modeParameters");
            }

            var modeChanges = new List<ChannelModeChangeInfo>();

            ChannelModeChangeAction action = ChannelModeChangeAction.Set;
            string[] parameters = modeParameters.Split(new char[] { ' ' });
            System.Collections.IEnumerator parametersEnumerator = parameters.GetEnumerator();
            // bring the enumerator to the 1. element
            parametersEnumerator.MoveNext();
            foreach (char modeChar in mode)
            {
                switch (modeChar)
                {
                    case '+':
                        action = ChannelModeChangeAction.Set;
                        break;

                    case '-':
                        action = ChannelModeChangeAction.Unset;
                        break;

                    default:
                        ChannelModeInfo modeInfo = null;
                        modeMap.TryGetValue(modeChar, out modeInfo);
                        if (modeInfo == null)
                        {
                            // modes not specified in CHANMODES are expected to
                            // always have parameters
                            modeInfo = new ChannelModeInfo((ChannelMode)modeChar, ChannelModeHasParameter.Always);
                        }

                        string parameter = null;
                        ChannelMode channelMode = modeInfo.Mode;
                        if (!Enum.IsDefined(typeof(ChannelMode), channelMode))
                        {
                            channelMode = ChannelMode.Unknown;
                        }
                        ChannelModeHasParameter hasParameter = modeInfo.HasParameter;
                        if (hasParameter == ChannelModeHasParameter.Always ||
                            (hasParameter == ChannelModeHasParameter.OnlySet &&
                             action == ChannelModeChangeAction.Set))
                        {
                            parameter = (string)parametersEnumerator.Current;
                            parametersEnumerator.MoveNext();
                        }

                        modeChanges.Add(new ChannelModeChangeInfo()
                        {
                            Action = action,
                            Mode = channelMode,
                            ModeChar = modeChar,
                            Parameter = parameter
                        });
                        break;
                }
            }

            return modeChanges;
        }
    }

    public class ChannelModeInfo
    {
        public ChannelModeInfo(ChannelMode mode, ChannelModeHasParameter hasParameter)
        {
            this.Mode = mode;
            this.HasParameter = hasParameter;
        }

        public ChannelModeHasParameter HasParameter { get; set; }
        public ChannelMode Mode { get; set; }
    }

    public class ChannelModeMap : Dictionary<char, ChannelModeInfo>
    {
        // TODO: verify RFC modes!
        public ChannelModeMap() :
            // Smuxi mapping
            this("oqahvbeI,k,l,imnpstr")
        // IRCnet mapping
        //this("beIR,k,l,imnpstaqr")
        {
        }

        public ChannelModeMap(string channelModes) => this.Parse(channelModes);

        public void Parse(string channelModes)
        {
            string listAlways = channelModes.Split(',')[0];
            string settingAlways = channelModes.Split(',')[1];
            string onlySet = channelModes.Split(',')[2];
            string never = channelModes.Split(',')[3];

            foreach (char mode in listAlways)
            {
                this[mode] = new ChannelModeInfo((ChannelMode)mode, ChannelModeHasParameter.Always);
            }
            foreach (char mode in settingAlways)
            {
                this[mode] = new ChannelModeInfo((ChannelMode)mode, ChannelModeHasParameter.Always);
            }
            foreach (char mode in onlySet)
            {
                this[mode] = new ChannelModeInfo((ChannelMode)mode, ChannelModeHasParameter.OnlySet);
            }
            foreach (char mode in never)
            {
                this[mode] = new ChannelModeInfo((ChannelMode)mode, ChannelModeHasParameter.Never);
            }
        }
    }
}
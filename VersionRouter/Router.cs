﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mark.VersionRoute
{
    public class Router
    {
        private List<Package> _packages;
        private List<Group> _groups;

        public Router(List<Package> packages, List<Group> groups)
        {
            _packages = packages;
            _groups = groups;
        }

        public Entry Match(string nativeName, string nativeVersion, string uid)
        {
            List<Entry> entries = new List<Entry>();
            _packages.ForEach(x =>
            {
                x.Client.ForEach(n =>
                {
                    var entry = new Entry(n.Name, n.User, n.Group, n.Url, n.Version, x.Version);
                    entries.Add(entry);
                });
            });

            List<Entry> namedNative = entries.Where(x => VerifyName(x, nativeName)).ToList();

            var resultList = namedNative.Where(x =>
                Entry.ParseVersion(nativeVersion) >= x.ClientVersion
                && VerifyUser(x, uid, _groups)).ToList();
            var resultItem = resultList.OrderByDescending(x => x.ClientVersion).FirstOrDefault();
            if (resultItem != null)
            {
                resultItem = resultList.Where(x => x.ClientVersion == resultItem.ClientVersion)
                    .OrderByDescending(x => x.PackageVersion)
                    .FirstOrDefault();
            }

            return resultItem == null ? namedNative.FirstOrDefault() : resultItem;
        }

        private bool VerifyName(Entry entry, string name)
        {
            if (string.IsNullOrWhiteSpace(entry.Name) || entry.Name == "*")
                return true;
            else
                return string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        private bool VerifyUser(Entry entry, string user, List<Group> groupList)
        {
            if (user == null) user = "";
            if (!string.IsNullOrWhiteSpace(entry.User))
            {
                if (entry.User == "*") return true;
                if (entry.User.Split(',').Any(x => x.Equals(user, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            if (!string.IsNullOrWhiteSpace(entry.Group) && groupList != null && groupList.Count > 0)
            {
                if (entry.Group == "*" && groupList.Any(x => x.Users.Any(u => user.Equals(u, StringComparison.OrdinalIgnoreCase))))
                    return true;

                var group = groupList.FirstOrDefault(x => x.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                if (group != null && group.Users.Any(x => user.Equals(x, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }

    }
}
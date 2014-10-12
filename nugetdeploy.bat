nuget pack SysCache\NHibernate.Caches.SysCache\NHibernate.Caches.SysCache.nuspec
nuget push -source http://packages.nuget.org/v1/ NHibernate.Caches.SysCache.4.0.1.4000.nupkg %1

nuget pack SysCache2\NHibernate.Caches.SysCache2\NHibernate.Caches.SysCache2.nuspec
nuget push -source http://packages.nuget.org/v1/ NHibernate.Caches.SysCache2.4.0.1.4000.nupkg %1
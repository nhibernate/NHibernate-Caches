nuget pack SysCache\NHibernate.Caches.SysCache\NHibernate.Caches.SysCache.nuspec
nuget push -source http://packages.nuget.org/v1/ NHibernate.Caches.SysCache.5.0.0.nupkg %1

nuget pack SysCache2\NHibernate.Caches.SysCache2\NHibernate.Caches.SysCache2.nuspec
nuget push -source http://packages.nuget.org/v1/ NHibernate.Caches.SysCache2.5.0.0.nupkg %1
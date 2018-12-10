## NHibernate.Caches [![Build status](https://ci.appveyor.com/api/projects/status/42rw3tks2mg6vxvk/branch/master?svg=true)](https://ci.appveyor.com/project/nhibernate/nhibernate-caches/branch/master)

[Documentation](http://nhibernate.info/doc/nhibernate-reference/caches.html) and examples are available at http://nhibernate.info/. 
Any feedback or issue can be sent to NHibernate user group ([http://groups.google.com/group/nhusers][userGroup]) and will be greatly appreciated. 

Up-to-date source code available at [**GitHub Website**][nhGithub]

Website:
[https://github.com/nhibernate/NHibernate-Caches/][nhGithub]


## What is NHibernate.Caches ?


NHibernate is able to use external caching plugins to minimize the access to the database and improve the performance.
The NHibernate Contrib contains several packages to work with different caching servers and frameworks. 
It's recommended to research for a while before deciding which one is better for you, since some providers require installing additional services 
(which provides an awesome performance, but might be harder to install in some scenarios).


## Notes

#### Build 5.5.1 for NHibernate 5.2.0

Re-release in order to have proper release notes in the NuGet packages.

#### Build 5.5.0 for NHibernate 5.2.0

NHibernate 5.2.0 is targeted by all cache providers.

* Bug
    * #53 - Disable append-hashcode when library is not built for netFx
    * #32 - Key computing for Memcached leaks hashers

* New feature
    * #50 - Common cache serializer and json serializer
    * #45 - Add Redis provider with batching support

* Improvement
    * #52 - Cease using cryptographic hashes

* Task
    * #56 - Upgrade to NH 5.2.0

#### Build 5.4.0 for NHibernate 5.1.0

NHibernate 5.1.0 is targeted by all cache providers.

* Bug
    * #48 - Avoid undue cache misses

* New feature
    * #47 - Add an option for appending hashcode to key
    * #38 - Add a Core RtMemoryCache

* Improvement
    * #36 - Push symbols packages

* Task
    * #46 - Update NHibernate to 5.1.0

#### Build 5.3.2 for NHibernate 5.0.0/5.1.0

* Bug
    * #40 NullReferenceException in SysCache2's SysCacheProvider

#### Build 5.3.1 for NHibernate 5.0.0/5.1.0

* Bug
    * #37 Fix CoreDistributedCache.Memory package description

#### Build 5.3.0 for NHibernate 5.0.0/5.1.0

NHibernate 5.1.0 is targeted by new cache providers.

* Bug
    * #27 Documentation fixes

* New Feature
    * #25 Add a .Net Core MemoryCache
    * #28 Add a .Net Core DistributedCache

* Improvement
    * #33 Generate XML comment documentation
    * #34 Build release artifacts on AppVeyor

#### Build 5.2.0 for NHibernate 5.0.0
* Bug
    * #19 - Partially configured regions do not fallback on defaults

* Improvement
    * #20 - Modernize locking in SysCache2
    * #24 - Generates ICache async counter-parts instead of hand coding them

#### Build 5.1.0 for NHibernate 5.0.0
* Bug
    * [NHCH-25] - TransactionScope promotes SysCache2 command dependency to a distributed transaction
    * [NHCH-43] - QueryCache CJK language not supported
    * [NHCH-51] - EnyimMemcached cannot be used by many session factories
    * [NHCH-53] - RtMemoryCache accepts invalid priorities

* New Feature
    * [NHCH-38] - add useSlidingExpiration property to choose between absolute or sliding expiration

* Improvement
    * [NHCH-50] - Non-compliant absolut expiration
    * [NHCH-52] - Add default expiration support to SysCache2

#### Build 5.0.0 for NHibernate 5.0.0
* Breaking changes
    * All cache providers work with .NET 4.6.1

#### Build 4.0.1GA for NHibernate 4.0.1GA

#### Build 3.0.0.GA (rev1442 of contrib) for NHibernate 3.0.0CR1

#### Build 3.0.0.CR (rev1440 of contrib) for NHibernate 3.0.0CR1

#### Build 3.0.0.Beta (rev1421 of contrib) for NHibernate 3.0.0Beta2

#### Build 3.0.0.Beta1 (rev1341 of contrib) for NHibernate 3.0.0Alpha3

#### Build 3.0.0.Alpha2 (rev1318 of contrib) for NHibernate 3.0.0Alpha2
* Breaking changes
    * All cache providers work with .NET3.5

** Improvement
    * [NHCH-24] - Strong naming and signing assemblies
    * [NHCH-26] - NHibernate.Caches.Memcached updated to use Enyim.Caching.Memcached
    * [NHCH-28] - Remove dependency to any logging framework


#### Build 3.0.0.Alpha1 (rev1302 of contrib) for NHibernate 3.0.0Alpha1
** Bug
    * [NHCH-27] - MemCache provider fails to delete item from cache if server is memcached 1.4.4

#### Build 2.1.2.GA (rev1204 of contrib) for NHibernate 2.1.2GA

#### Build 2.1.1.GA (rev1104 of contrib) for NHibernate 2.1.1GA

#### Build 2.1.0.GA (rev930 of contrib) for NHibernate 2.1.0GA

#### Build 2.1.0.Alpha1 (rev887 of contrib) for NHibernate 2.1.0Beta3
** Improvement
    * [NHCH-21] - Allow mnemonic values for priority


** Patch
    * [NHCH-19] - Duplicate expiration property handling in MemCacheClient
    * [NHCH-22] - Enable read of cache.default_expiration from NH configuration

#### Build 2.0.0.GA for NHibernate 2.0.1GA
- In the last release (2.0.0.RC1) there was an error in the documentation about the NH version, this release fixes that minor bug.




[nhGithub]:https://github.com/nhibernate/NHibernate-Caches
[userGroup]:http://groups.google.com/group/nhusers

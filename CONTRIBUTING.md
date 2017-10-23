# Contributing to NHibernate.Caches

This document describes the policies and procedures for working with NHibernate.Caches. It also describes how to quickly get going with a test case and optionally a fix for NHibernate.Caches.

For the least friction, please follow the steps in the order presented, being careful not to miss any. There are many details in this document that will help your contribution go as smoothly as possible. Please read it thoroughly.

## Create or find a Jira Issue

Jira is used to generate the release notes and serves as a central point of reference for all changes that have occurred to NHibernate.Caches.

Visit [https://nhibernate.jira.com/projects/NHCH/issues][1] and search for your issue. If you see it, giving it a vote is a good way to increase the visibility of the issue.

Before creating an issue, please do your best to verify the existence of the problem. This reduces noise in the issue tracker and helps conserve the resources of the team for more useful tasks. Note the issue number for future steps.

## Fork and Clone from GitHub

The main GitHub repository is at [https://github.com/nhibernate/NHibernate-Caches/][2]. If you plan to contribute your test case or improvement back to NHibernate.Caches, you should visit that page and fork the repository so you can commit your own changes and then submit a pull request.

## The Builds

**Nant** builds are defined in the root of the repository.

1.  Run **nant generate-async** to generate async counterparts of any test you have added or changed.
2.  Run **nant test** to run the tests. Cache provider tests depending on a cache server does not fail the task by default, check the nant output for unexpected failures.
3.  **GaRelease.bat** will create the release package.

## Creating a Test Case to Verify the Issue

In most cases, you will be adding your test to the NHibernate.Caches.**ProviderName**.Tests project. (Where **ProviderName** is the name of the cache provider your are working with.)

Edit the test as you see fit. Test it. Don't commit yet; there are details in a later step.

## Regenerate async code

NHibernate.Caches uses a code generator for its async tests. If your changes, including tests, involve any synchronous method having an async counter-part, you should regenerate the async code. Run **nant generate-async** for this from the root of NHibernate.Caches repository. Then test any async counter-part it may have generated from your tests.

## Commit your Test Case

Ensure that your e-mail address and name are configured appropriately in Git.

Create a feature branch so it's easy to keep it separate from other improvements. Having a pull request accepted might involve further commits based on community feedback, so having the feature branch provides a tidy place to work from. Using the issue number as the branch name is good practice.

## Implementing the Bug Fix or Improvement

Since you now have a failing test case, it should be straight-forward to step into NHibernate.Caches to attempt to ascertain what the problem is. While this may seem daunting at first, feel free to give it a go. It's just code after all. :)

### Ensure All Tests Pass

Once you've made changes to the NHibernate.Caches code base, you'll want to ensure that you haven't caused any previously passing tests to fail. The easiest way to check this is to run **nant test** from the root of NHibernate.Caches repository.

Cache provider tests depending on a cache server does not fail the task by default. If you have change such a provider, setup its server and check the nant output for unexpected failures.

## Submit a Pull Request

If you are fixing an existing issue, please make sure to include this issue number in your GitHub pull request. 

We use tabs for code indentation, not spaces. As this is not the default in Visual Studio, you will need to reconfigure Visual Studio to indent with tabs whenever you work on the NHibernate.Cahces codebase. To make this easier, NHibernate.Caches has an [editorconfig][3] configuration file to switch Visual Studio automatically between tabs and spaces mode. It is recommended you install editorconfig from the Visual Studio Extension Manager.

After submitting your pull request, come back later to check the outcome of automated builds. If some have failed, they will be listed in your pull request with a link to the corresponding AppVeyor build. Find out in the build which tests are newly failing, and take appropriate action.

## Further Discussion

The NHibernate team monitors GitHub regularly, so your request will be noticed. If you want to discuss it further, you are welcome to post to the [nhibernate-development mailing list][4].

## Happy Contributing!

The NHibernate community values your contributions. Thank you for the time you have invested.

 [1]: https://nhibernate.jira.com/projects/NHCH/issues
 [2]: https://github.com/nhibernate/NHibernate-Caches/
 [3]: http://www.editorconfig.org/
 [4]: http://groups.google.com/group/nhibernate-development

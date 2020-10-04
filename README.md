# git-historical-cloc

This is a .NET Core command line utility that runs [CLOC](http://cloc.sourceforge.net) on each commit of a git repo and spits out a CSV file.  The results CSV file has one column for each language found.  It ignores blank lines & comments.  The CSV file can be charted with something like Google Sheets to see the repo change over time.

I'm sure this utility already exists somewhere in some form, but the easiest way to find it was probably to write my own and wait for people to tell me about it :upside_down_face:

:warning: **Note that it will modify the target repo (see notes below).**  

## License

MIT

## Requirements

* .NET Core 3.1
* `cloc` needs to be in your path.  Installing it with your favorite package manager should be enough (homebrew, Chocolatey, etc)

## Usage

* Clone the repo.  
* There are two runner scripts for Windows (`run.cmd`) and UNIX (`run.sh`).  Both take a single parameter that is a path to a git repository to analyze.
* The results will be output as `results.csv` in the current directory.

## :warning: What it does to the target repo

* All untracked files in the target repo will be blown away.
* The utility will make a new temporary branch and hard-reset it to every commit on the `master` branch.  The temporary branch will be removed when done and `master` will be checked out.

## TODO

* Allow specifying other branches
* Allow specifying an output filename
* More error handling (is the given directory actually a git repo?)
* More protections for the target repo (check that there are no unstaged changes, etc)
* Spit out a PNG directly from this app instead of just a CSV file

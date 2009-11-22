cd SysCache
nant -D:project.config=release -t:net-3.5
cd ..

cd SysCache2
nant -D:project.config=release -t:net-3.5
cd ..

cd SharedCache
nant -D:project.config=release -t:net-3.5
cd ..

cd MemCache
nant -D:project.config=release -t:net-3.5
cd ..

cd Velocity
nant -D:project.config=release -t:net-3.5
cd ..

cd Prevalence
nant -D:project.config=release -t:net-3.5
cd ..

pause
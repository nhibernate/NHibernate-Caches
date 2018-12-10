local version = redis.call('incr', KEYS[1])
if version > tonumber(ARGV[1]) then
	version = 1
	redis.call('set', KEYS[1], version)
end
return version

redis.call('publish', ARGV[1], ARGV[2])
return redis.call('del', KEYS[1])

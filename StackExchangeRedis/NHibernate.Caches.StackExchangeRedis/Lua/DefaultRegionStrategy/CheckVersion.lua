local version = redis.call('get', KEYS[#KEYS])
if version ~= ARGV[#ARGV] then
	return redis.error_reply('Invalid version')
end

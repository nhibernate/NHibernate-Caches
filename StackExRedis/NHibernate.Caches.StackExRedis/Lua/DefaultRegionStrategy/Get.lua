local value = redis.call('get', KEYS[1])
if value ~= nil and ARGV[1] == '1' then
	redis.call('pexpire', KEYS[1], ARGV[2])
end
return value

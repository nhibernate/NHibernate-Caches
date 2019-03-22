local expirationMs = ARGV[2]
local values = {}
for i=1,#KEYS do
	values[i] = redis.call('get', KEYS[i])
	if values[i] ~= nil then
		redis.call('pexpire', KEYS[i], expirationMs)
	end
end
return values

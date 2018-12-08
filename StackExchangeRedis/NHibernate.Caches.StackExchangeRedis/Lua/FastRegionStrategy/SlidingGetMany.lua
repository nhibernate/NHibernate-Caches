local expirationMs = ARGV[2]
local values = redis.call('MGET', unpack(KEYS));
for i=1,#KEYS do
	if values[i] ~= nil then
		redis.call('pexpire', KEYS[i], expirationMs)
	end
end
return values

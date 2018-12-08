local values = {{}}
local sliding = ARGV[#ARGV-2]
local expirationMs = ARGV[#ARGV-1]
for i=1,#KEYS-1 do
	local value = redis.call('get', KEYS[i])
	if value ~= nil and sliding == '1' then
		redis.call('pexpire', KEYS[i], expirationMs)
	end
	values[i] = value
end
return values

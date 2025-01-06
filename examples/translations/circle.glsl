//This is a file that could work with shadertoy, translated with FragRoom
//https://www.shadertoy.com/view/4XKyW1

//Translated using FragRoom version v1.4.1 - https://github.com/Dumbelfo08/FragRoom

vec4 TRANSLATOR_RT_fragColor;
vec2 TRANSLATOR_RT_fragCoord;

  

vec3 palette(in float t){ //RGB palette in function of time
	float r = sin(t) * 0.5 + 0.5; //map the sine function to [0,1]
	float g = sin(t + 2.0*3.14*0.333) * 0.5 + 0.5; //map the sine function to [0,1] and introduces 120 degree phase
	float b = sin(t + 2.0*3.14*0.666) * 0.5 + 0.5; //map the sine function to [0,1] and introduces 240 degree phase
	
	return vec3(r, g, b);
}

void mainMethod(){
	vec2 coords = TRANSLATOR_RT_fragCoord;
	coords.x *= iResolution.x/iResolution.y; //Fix the aspect ratio
	
	float dist = length(coords); //calcultae distance to center
	float factor = 1.0 - step(sin(0.6*iTime)*0.4+0.6, dist); //calculate the factor with reverse step function
	
	vec3 color = palette(iTime) * factor; //Multiply by factor so only circe is colored
    TRANSLATOR_RT_fragColor = vec4(color, 1.0); //output
}
void mainImage(out vec4 fragColor, in vec2 fragCoord){
    TRANSLATOR_RT_fragCoord = ((fragCoord - 0.5) / (iResolution.xy - 1.0)) * 2.0 - 1.0;
    mainMethod();
    fragColor = TRANSLATOR_RT_fragColor;
}
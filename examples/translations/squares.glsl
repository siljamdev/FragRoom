//This is a file that could work with shadertoy, translated with FragRoom
//https://www.shadertoy.com/view/43VyW1

//Translated using FragRoom version v1.4.1 - https://github.com/siljamdev/FragRoom

vec4 TRANSLATOR_RT_fragColor;
vec2 TRANSLATOR_RT_fragCoord;

  

void mainMethod(){   
    vec2 pixs = vec2(16.0); //Square size, in pixels
    vec2 sqn = iResolution.xy/pixs; //Number of squares
    
    vec2 corner = TRANSLATOR_RT_fragCoord/2.0+0.5; //Coordinates, with the corner in the bottom left
    vec2 square = vec2(floor(corner.x*sqn.x),floor(corner.y*sqn.y)); //Coordinates, made into the bigger squares
    
    vec3 col = vec3(sin(square.x-8.0*iTime-square.y*square.y),cos(square.y+8.0*iTime+square.x*square.x),0.0); //Calcultae the color
    TRANSLATOR_RT_fragColor = vec4(col, 1.0); //output
}
void mainImage(out vec4 fragColor, in vec2 fragCoord){
    TRANSLATOR_RT_fragCoord = ((fragCoord - 0.5) / (iResolution.xy - 1.0)) * 2.0 - 1.0;
    mainMethod();
    fragColor = TRANSLATOR_RT_fragColor;
}
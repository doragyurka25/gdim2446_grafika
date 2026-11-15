#version 330 core
out vec4 FragColor;

uniform vec3 uAmbientStrength;
uniform vec3 uDiffuseStrength;
uniform vec3 uSpecularStrength;
uniform vec3 uAmbientColor;

uniform vec3 uLightColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;

uniform float uShininess;

		
in vec4 outCol;
in vec3 outNormal;
in vec3 outWorldPosition;

void main()
{
    vec3 objectColor = outCol.rgb;

    vec3 ambient = uAmbientStrength * uAmbientColor * objectColor;

    vec3 lightDir = normalize(uLightPos - outWorldPosition);
    vec3 normal = normalize(outNormal);
    vec3 viewDir = normalize(uViewPos - outWorldPosition);

    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = uDiffuseStrength * diff * uLightColor * objectColor;

    vec3 reflectDir = reflect(-lightDir, normal);

    float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);

    vec3 specular = uSpecularStrength * spec * uLightColor;

    vec3 res = ambient + diffuse + specular;

    FragColor = vec4(res, outCol.a);
}
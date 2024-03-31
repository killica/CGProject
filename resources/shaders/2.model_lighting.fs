#version 330 core

layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec4 BrightColor;

struct DirLight {
    vec3 direction;

    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

struct PointLight {
    vec3 position;

    vec3 specular;
    vec3 diffuse;
    vec3 ambient;

    float constant;
    float linear;
    float quadratic;
};

struct SpotLight {
    vec3 position;
    vec3 direction;

    float cutOff;
    float outerCutOff;

    vec3 specular;
    vec3 diffuse;
    vec3 ambient;

    float constant;
    float linear;
    float quadratic;

    int ind;
};

struct Material {
    sampler2D texture_diffuse1;
    sampler2D texture_specular1;

    float shininess;
};
in vec2 TexCoords;
in vec3 Normal;
in vec3 FragPos;

uniform DirLight dirLight;
uniform PointLight pointLight;
uniform SpotLight spotLight;
uniform Material material;

uniform vec3 viewPosition;
uniform bool blinn;

vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir);
vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir);
vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir);

void main()
{
    vec3 normal = normalize(Normal);
    vec3 viewDir = normalize(viewPosition - FragPos);
    vec3 result = CalcDirLight(dirLight, normal, viewDir);
    result += CalcPointLight(pointLight, normal, FragPos, viewDir);
    result += CalcSpotLight(spotLight, normal, FragPos, viewDir);
    // check whether result is higher than some threshold, if so, output as bloom threshold color
    float brightness = dot(result, vec3(0.2126, 0.7152, 0.0722));
    if(brightness > 1.0)
        BrightColor = vec4(result, 1.0);
    else
        BrightColor = vec4(0.0, 0.0, 0.0, 1.0);
    FragColor = vec4(result, 1.0);
}

vec3 CalcDirLight(DirLight dir, vec3 normal, vec3 viewDir) {
    vec3 lightDir = normalize(-dir.direction);
    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = 0.0f;
    //Blinn-Phong
    if(blinn){
        vec3 halfwayDir = normalize(lightDir + viewDir);
        spec = pow(max(dot(normal, halfwayDir),0.0), material.shininess);
    }else{
        spec = pow(max(dot(viewDir, reflectDir),0.0), material.shininess);
    }
    // combine results
    vec3 ambient = dir.ambient * vec3(texture(material.texture_diffuse1, TexCoords));
    vec3 diffuse = dir.diffuse * diff * vec3(texture(material.texture_diffuse1, TexCoords));
    vec3 specular = dir.specular * spec * vec3(texture(material.texture_specular1, TexCoords));
    return (ambient + diffuse + specular);
}

vec3 CalcPointLight(PointLight point, vec3 normal, vec3 fragPos, vec3 viewDir) {
    vec3 lightDir = normalize(point.position - fragPos);
    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    // attenuation
    float distance = length(point.position - fragPos);
    float attenuation = 1.0 / (point.constant + point.linear * distance + point.quadratic * (distance * distance));
    // combine results
    vec3 ambient = point.ambient * vec3(texture(material.texture_diffuse1, TexCoords));
    vec3 diffuse = point.diffuse * diff * vec3(texture(material.texture_diffuse1, TexCoords));
    vec3 specular = point.specular * spec * vec3(texture(material.texture_specular1, TexCoords));
    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;
    return (ambient + diffuse + specular);
}

// calculates the color when using a spot light.
vec3 CalcSpotLight(SpotLight spot, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(spot.position - fragPos);
    float cosTheta = dot(normalize(spot.direction), -lightDir);
    float epsilon = spot.cutOff - spot.outerCutOff;
    float intensity = clamp((cosTheta - spot.outerCutOff) / epsilon, 0.0, 1.0);

    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    // attenuation
    float distance = length(spot.position - fragPos);
    float attenuation = 1.0 / (spot.constant + spot.linear * distance + spot.quadratic * (distance * distance));
    // combine results
    vec3 ambient = spot.ambient * texture(material.texture_diffuse1, TexCoords).rgb;
    vec3 diffuse = spot.diffuse * diff * texture(material.texture_diffuse1, TexCoords).rgb;
    vec3 specular = spot.specular * spec * texture(material.texture_specular1, TexCoords).rgb;

    diffuse *= attenuation * intensity;
    specular *= attenuation * intensity;
    return (ambient + diffuse + specular) * spot.ind;

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenTK.Compute.OpenCL;
using OpenTK.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace WinForms_TestApp
{
    public class Shader
    {
        public int ProgramID { get; private set; }
        public readonly Dictionary<string, int> AttributeLocations = [];
        public readonly Dictionary<string, int> UniformLocations = [];
        public Shader(string vertexCode, string fragmentCode) 
        {
            ProgramID = GL.CreateProgram();

            int vertexShaderID = LoadShader(ShaderType.VertexShader, vertexCode);
            GL.AttachShader(ProgramID, vertexShaderID);
            GL.DeleteShader(vertexShaderID);

            int fragmentShaderID = LoadShader(ShaderType.FragmentShader, fragmentCode);
            GL.AttachShader(ProgramID, fragmentShaderID);
            GL.DeleteShader(fragmentShaderID);

            GL.LinkProgram(ProgramID);
            GL.UseProgram(ProgramID);

            GL.GetProgram(ProgramID, GetProgramParameterName.LinkStatus, out int linkSuccess);

            if(linkSuccess == 0)
            {
                string info = GL.GetProgramInfoLog(ProgramID);
                throw new Exception("Failed to link program! Reason: " + info);
            }

            string pattern = @"(uniform|in)\s+\S+\s+(\S+)\s*;";
            MatchCollection matches = Regex.Matches(vertexCode + '\n' + fragmentCode, pattern);
            foreach (Match match in matches) 
            {
                string name = match.Groups[2].Value;
                switch (match.Groups[1].Value)
                {
                    case "uniform":
                    {
                        UniformLocations.Add(name, GL.GetUniformLocation(ProgramID, name));
                        break;
                    }
                    case "in":
                    {
                        AttributeLocations.Add(name, GL.GetAttribLocation(ProgramID, name));
                        break;
                    }
                }
            }
        }
        public void UseShader()
        {
            GL.UseProgram(ProgramID);
        }
        private static int LoadShader(ShaderType type, string shaderCode)
        {
            int shaderID = GL.CreateShader(type);
            GL.ShaderSource(shaderID, shaderCode);
            GL.CompileShader(shaderID);
            GL.GetShader(shaderID, ShaderParameter.CompileStatus, out int compileSuccess);
            if (compileSuccess == 0)
            {
                string info = GL.GetShaderInfoLog(shaderID);
                throw new Exception("Failed to compile shader! Reason: " + info);
            }
            return shaderID;
        }
    }
}

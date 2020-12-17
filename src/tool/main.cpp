// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#include "stdinc.hpp"

auto overwrite_prompt(const std::string& file) -> bool
{
    auto overwrite = true;

    if (std::filesystem::exists(file))
    {
        do
        {
            printf("File \"%s\" already exists, overwrite? [Y/n]: ", file.data());
            auto result = std::getchar();

            if (result == '\n' || result == 'Y' || result == 'y')
            {
                break;
            }
            else if (result == 'N' || result == 'n')
            {
                overwrite = false;
                break;
            }
        } while (true);
    }

    return overwrite;
}

void assemble_file(lui::assembler& assembler, std::string file)
{
    const auto ext = std::string(".luasm");
    const auto extpos = file.find(ext);
    
    if (extpos != std::string::npos)
    {
        file.replace(extpos, ext.length(), "");
    }

    auto data = utils::file::read(file + ".luasm");

    assembler.assemble(data);
    
    utils::file::save(file + ".luac", assembler.output());
}

using namespace std::filesystem;

void disassemble_dir(lui::disassembler& disassembler, const std::filesystem::path& dir_path)
{
    if (!std::filesystem::exists(dir_path)) 
        return;

    for (const directory_entry& entry : recursive_directory_iterator(dir_path))
    {
        auto path = entry.path();

        if (is_directory(entry.status()) && std::string(path).find(".DS_Store") != std::string::npos)
        {
            //recurse_dir(disassembler, entry.path());
            continue;
        }
        else if(entry.is_regular_file() && std::string(path).find(".luac") != std::string::npos)
        {

            auto file = std::string(path);
            const auto ext = std::string(".luac");
            const auto extpos = file.find(ext);
            
            if (extpos != std::string::npos)
            {
                file.replace(extpos, ext.length(), "");
            }

            auto data = utils::file::read(file + ".luac");

            disassembler.disassemble(data);
            LOG_INFO("%s disasembled.", file.data());
           // utils::file::save(file + ".luasm", disassembler.output());
        }
    }
}

void disassemble_file(lui::disassembler& disassembler, std::string file)
{
    if(std::filesystem::is_directory(file))
    {
        disassemble_dir(disassembler, file);
        return;
    }

    const auto ext = std::string(".luac");
    const auto extpos = file.find(ext);
    
    if (extpos != std::string::npos)
    {
        file.replace(extpos, ext.length(), "");
    }

    auto data = utils::file::read(file + ".luac");

    disassembler.disassemble(data);
    
    utils::file::save(file + ".luasm", disassembler.output());
}

void decompile_file(lui::disassembler& disassembler, lui::decompiler& decompiler, std::string file)
{
    if(std::filesystem::is_directory(file))
    {
        disassemble_dir(disassembler, file);
        return;
    }

    const auto ext = std::string(".luac");
    const auto extpos = file.find(ext);
    
    if (extpos != std::string::npos)
    {
        file.replace(extpos, ext.length(), "");
    }

    auto data = utils::file::read(file + ".luac");

    disassembler.disassemble(data);

    decompiler.decompile(disassembler.output_d());
    
    utils::file::save(file + ".lua", decompiler.output());
}

int parse_flags(int argc, char** argv, game& game, mode& mode)
{
    if (argc != 4) return 1;

    std::string arg = utils::string::to_lower(argv[1]);

    if (arg == "-iw6")
    {
        game = game::IW6;
    }
    else
    {
        printf("Unknown game \"%s\".\n", argv[1]);
        return 1;
    }

    arg = utils::string::to_lower(argv[2]);

    if (arg == "-asm")
    {
        mode = mode::ASM;
    }
    else if (arg == "-disasm")
    {
        mode = mode::DISASM;
    }
    else if(arg == "-decomp")
    {
        mode = mode::DECOMP;
    }
    else
    {
        printf("Unknown mode \"%s\".\n\n", argv[2]);
        return 1;
    }

    return 0;
}

int main(int argc, char** argv)
{
    std::string file = argv[argc - 1];
    mode mode = mode::__;
    game game = game::__;

    if (parse_flags(argc, argv, game, mode))
    {
        printf("usage: lui-tool.exe <game> <mode> <file>\n");
        printf("	- games: -iw6\n");
        printf("	- modes: -disasm\n");
        return 0;
    }

    if (mode == mode::ASM)
    {
        if (game == game::IW6)
        {
            IW6::assembler assembler;
            assemble_file(assembler, file);
        }
    }
    else if (mode == mode::DISASM)
    {
        if (game == game::IW6)
        {
            IW6::disassembler disassembler;
            disassemble_file(disassembler, file);
        }
    }
    else if(mode == mode::DECOMP)
    {
        if (game == game::IW6)
        {
            IW6::disassembler disassembler;
            IW6::decompiler decompiler;
            decompile_file(disassembler, decompiler, file);
        }
    }

    return 0;
}
